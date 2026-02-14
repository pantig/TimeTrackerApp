using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using TimeTrackerApp.Models;

namespace TimeTrackerApp.Services
{
    public class ExcelExportService
    {
        public byte[] GenerateMonthlyReport(
            string employeeName,
            int year,
            int month,
            List<TimeEntry> entries,
            Dictionary<DateTime, DayMarker?> dayMarkers)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Raport miesięczny");

            var culture = new CultureInfo("pl-PL");
            var monthName = new DateTime(year, month, 1).ToString("MMMM", culture);

            // Nagłówek
            worksheet.Cells["D1"].Value = employeeName;
            worksheet.Cells["D1"].Style.Font.Bold = true;
            worksheet.Cells["D1"].Style.Font.Size = 14;

            worksheet.Cells["D2"].Value = string.Format("MIESIĘCZNY RAPORT PRACY - {0}", monthName);
            worksheet.Cells["D2"].Style.Font.Bold = true;
            worksheet.Cells["D2"].Style.Font.Size = 12;

            // Nagłówki kolumn (wiersz 8)
            var headers = new[] { "Dzień tygodnia", "Data", "Uwagi", "Opis", "Przedział godzinowy", "", "Projekt", "Czynności wykonywane", "Ilość godzin", "Godziny w dniu" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[8, i + 1].Value = headers[i];
                worksheet.Cells[8, i + 1].Style.Font.Bold = true;
                worksheet.Cells[8, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[8, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                worksheet.Cells[8, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Grupowanie wpisów po dniach
            var entriesByDay = entries
                .GroupBy(e => e.EntryDate.Date)
                .ToList();
            
            entriesByDay = entriesByDay.OrderBy(g => g.Key).ToList();

            int currentRow = 10; // Zaczynamy od wiersza 10
            var daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(year, month, day);
                var dayName = date.ToString("dddd", culture);
                var dayGroup = entriesByDay.FirstOrDefault(g => g.Key == date);
                var dayMarker = dayMarkers.ContainsKey(date) ? dayMarkers[date] : null;

                if (dayGroup != null && dayGroup.Any())
                {
                    // Dzień z wpisami
                    var dayEntries = dayGroup.ToList();
                    dayEntries = dayEntries.OrderBy(e => e.StartTime).ToList();
                    var totalHours = dayEntries.Sum(e => e.TotalHours);

                    for (int i = 0; i < dayEntries.Count; i++)
                    {
                        var entry = dayEntries[i];

                        if (i == 0)
                        {
                            // Pierwszy wpis - wyświetl dzień i datę
                            worksheet.Cells[currentRow, 1].Value = dayName;
                            worksheet.Cells[currentRow, 2].Value = date.ToString("dd.MM.yyyy");
                            worksheet.Cells[currentRow, 3].Value = dayMarker != null ? GetDayMarkerText(dayMarker.Type) : "";
                        }

                        worksheet.Cells[currentRow, 4].Value = entry.Description ?? "";
                        worksheet.Cells[currentRow, 5].Value = string.Format("{0:hh\\:mm} - {1:hh\\:mm}", entry.StartTime, entry.EndTime);
                        worksheet.Cells[currentRow, 7].Value = entry.Project?.Name ?? "(brak projektu)";
                        worksheet.Cells[currentRow, 8].Value = entry.Description ?? "";
                        worksheet.Cells[currentRow, 9].Value = (double)entry.TotalHours;

                        if (i == 0)
                        {
                            worksheet.Cells[currentRow, 10].Value = (double)totalHours;
                        }

                        currentRow++;
                    }
                }
                else if (dayMarker != null)
                {
                    // Dzień bez wpisów ale z markerem
                    worksheet.Cells[currentRow, 1].Value = dayName;
                    worksheet.Cells[currentRow, 2].Value = date.ToString("dd.MM.yyyy");
                    worksheet.Cells[currentRow, 3].Value = GetDayMarkerText(dayMarker.Type);
                    currentRow++;
                }
                else if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    // Zwykły dzień roboczy bez wpisów
                    worksheet.Cells[currentRow, 1].Value = dayName;
                    worksheet.Cells[currentRow, 2].Value = date.ToString("dd.MM.yyyy");
                    currentRow++;
                }
            }

            // Podsumowanie
            currentRow++;
            var totalMonthHours = entries.Sum(e => e.TotalHours);
            worksheet.Cells[currentRow, 7].Value = "Łącznie";
            worksheet.Cells[currentRow, 7].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 8].Value = (double)totalMonthHours;
            worksheet.Cells[currentRow, 8].Style.Font.Bold = true;
            worksheet.Cells[currentRow, 9].Value = (double)totalMonthHours;
            worksheet.Cells[currentRow, 9].Style.Font.Bold = true;

            // Statystyki
            currentRow += 2;
            worksheet.Cells[currentRow, 3].Value = "Rodzaj";
            worksheet.Cells[currentRow, 4].Value = "Dni";
            worksheet.Cells[currentRow, 6].Value = "Projekt";
            worksheet.Cells[currentRow, 8].Value = "Godzin";
            worksheet.Cells[currentRow, 9].Value = "Dni w delegacji";

            for (int col = 3; col <= 9; col++)
            {
                worksheet.Cells[currentRow, col].Style.Font.Bold = true;
            }

            currentRow++;

            // Zlicz markery
            var markerStats = dayMarkers.Values
                .Where(m => m != null)
                .GroupBy(m => m!.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            foreach (var stat in markerStats)
            {
                worksheet.Cells[currentRow, 3].Value = GetDayMarkerText(stat.Type);
                worksheet.Cells[currentRow, 4].Value = stat.Count;
                currentRow++;
            }

            // Godziny po projektach
            var projectStats = entries
                .GroupBy(e => e.Project?.Name ?? "(brak projektu)")
                .Select(g => new { Project = g.Key, Hours = g.Sum(e => e.TotalHours) })
                .ToList();
            
            projectStats = projectStats.OrderByDescending(p => p.Hours).ToList();

            currentRow = currentRow - markerStats.Count; // Wróć do początku statystyk
            foreach (var stat in projectStats)
            {
                worksheet.Cells[currentRow, 6].Value = stat.Project;
                worksheet.Cells[currentRow, 8].Value = (double)stat.Hours;
                currentRow++;
            }

            // Dostosuj szerokości kolumn
            for (int col = 1; col <= 10; col++)
            {
                worksheet.Column(col).AutoFit();
            }

            return package.GetAsByteArray();
        }

        private string GetDayMarkerText(DayType type)
        {
            return type switch
            {
                DayType.BusinessTrip => "Delegacja",
                DayType.DayOff => "Dzień wolny",
                DayType.Sick => "Choroba",
                DayType.Vacation => "Urlop",
                _ => ""
            };
        }
    }
}