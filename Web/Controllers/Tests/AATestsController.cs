using ApplicationCore.Models;
using ApplicationCore.Services;
using Microsoft.AspNetCore.Mvc;
using ApplicationCore.DataAccess;
using Infrastructure.Helpers;
using Microsoft.SqlServer.Dac;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.IO;
using OfficeOpenXml;

namespace Web.Controllers.Tests;

public class AATestsController : BaseTestController
{
   private readonly DefaultContext _defaultContext;
   private readonly IDocModelsService _docModelsService;
   public AATestsController(DefaultContext defaultContext, IDocModelsService docModelsService)
   {
      _defaultContext = defaultContext;
      _docModelsService = docModelsService;
   }
   [HttpGet]
   public async Task<ActionResult> Index()
   {
      
      return Ok();
   }
   void ExportUpdateStatus()
   {
      var flags = new List<int>() { 1, 3, 4 };
      var records = _docModelsService.GetKeepRecords(new List<int>()).ToList();

      ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
      using (var package = new ExcelPackage())
      {
         // Add a worksheet
         var worksheet = package.Workbook.Worksheets.Add("更新狀態");

         // Add headers to the worksheet
         worksheet.Cells[1, 1].Value = "舊系統公文號";
         worksheet.Cells[1, 2].Value = "新系統公文號";
         worksheet.Cells[1, 3].Value = "收創日期";
         worksheet.Cells[1, 4].Value = "承辦人";

         worksheet.Cells[1, 5].Value = "舊狀態";
         worksheet.Cells[1, 6].Value = "新狀態";


         worksheet.Cells[1, 7].Value = "主旨";

         for (int i = 0; i < records.Count; i++)
         {
            worksheet.Cells[i + 2, 1].Value = records[i].Old_Num;
            worksheet.Cells[i + 2, 2].Value = records[i].Num;
            worksheet.Cells[i + 2, 3].Value = records[i].Date;
            worksheet.Cells[i + 2, 4].Value = records[i].Person;


            worksheet.Cells[i + 2, 5].Value = records[i].Result;
            worksheet.Cells[i + 2, 6].Value = StatusText(records[i].Keep);

            worksheet.Cells[i + 2, 7].Value = records[i].Title;
         }

         // Auto-fit columns for neatness
         worksheet.Cells.AutoFitColumns();

         // Save the Excel file to the specified file path
         package.SaveAs(new FileInfo(@"c:\\test\\0920\\更新狀態.xlsx"));
      }
   }
   void ExportChangePerson()
   {
      var records = _docModelsService.GetChangePersonRecords().ToList();

      ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
      using (var package = new ExcelPackage())
      {
         // Add a worksheet
         var worksheet = package.Workbook.Worksheets.Add("變更承辦人");

         // Add headers to the worksheet
         worksheet.Cells[1, 1].Value = "舊系統公文號";
         worksheet.Cells[1, 2].Value = "新系統公文號";
         worksheet.Cells[1, 3].Value = "收創日期";

         worksheet.Cells[1, 4].Value = "原承辦人";
         worksheet.Cells[1, 5].Value = "新承辦人";

         worksheet.Cells[1, 6].Value = "主旨";
         
         
         for (int i = 0; i < records.Count; i++)
         {
            var source = _defaultContext.SourceModels.FirstOrDefault(x => x.Num == records[i].Num);
            worksheet.Cells[i + 2, 1].Value = records[i].Old_Num;
            worksheet.Cells[i + 2, 2].Value = records[i].Num;
            worksheet.Cells[i + 2, 3].Value = records[i].Date;

            worksheet.Cells[i + 2, 4].Value = source!.Person;
            worksheet.Cells[i + 2, 5].Value = records[i].NewPersonName;

            worksheet.Cells[i + 2, 6].Value = records[i].Title;
         }

         // Auto-fit columns for neatness
         worksheet.Cells.AutoFitColumns();

         // Save the Excel file to the specified file path
         package.SaveAs(new FileInfo(@"c:\\test\\0920\\變更承辦人.xlsx"));
      }
   }
   async Task SaveSource()
   {
      ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
      var docs = new List<SourceModel>();
      string filepath = @"c:\\0920\\doc_source.xlsx";
      using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filepath)))
      {
         var worksheet = package.Workbook.Worksheets[1];
         if (worksheet == null)
         {
            ModelState.AddModelError("file", "無法讀取工作表");
            return;
         }

         var rowCount = worksheet.Dimension.Rows;
         for (int row = 2; row <= rowCount; row++)
         {
            var doc = new SourceModel()
            {
               Num = worksheet.Cells[row, 1].Text,
               Old_Num = worksheet.Cells[row, 2].Text,
               Old_CNum = worksheet.Cells[row, 3].Text,
               Unit = worksheet.Cells[row, 4].Text,
               Date = worksheet.Cells[row, 5].Text,
               Person = worksheet.Cells[row, 6].Text,
               Result = worksheet.Cells[row, 7].Text,
               Title = worksheet.Cells[row, 8].Text
            };
            docs.Add(doc);
         }

      }
      int batchSize = 1000;
      for (int i = 0; i < docs.Count; i += batchSize)
      {
         // Get the next batch (take a maximum of `batchSize` items)
         var batch = docs.Skip(i).Take(batchSize).ToList();

         // Add the batch to the DbContext
         _defaultContext.SourceModels.AddRange(batch);

         // Save changes asynchronously to the database for the current batch
         await _defaultContext.SaveChangesAsync();

         // Clear the tracked entities in the DbContext to avoid memory overhead
         _defaultContext.ChangeTracker.Clear();
      }
   }
   //[HttpGet("migrate")]
   //public ActionResult Migrate()
   //{

   //   _defaultContext.Database.Migrate();

   //   return Ok();
   //}
   //[HttpGet("cp")]
   //public ActionResult ChangePersonPs()
   //{
   //   var records = _docModelsService.GetChangePersonRecords();
   //   foreach (var record in records)
   //   {
   //      record.Ps = $"此案請轉新承辦人：{record.NewPersonName} (代號{record.NewPersonId})";
   //      _defaultContext.DocModels.Update(record);
   //   }
   //   _defaultContext.SaveChanges();
   //   return Ok(records);
   //}
   string RemoveStartingDoubleQuote(string input)
   {
      if (input.StartsWith("\""))
      {
         return input.Substring(1); // Remove the first character (double quote)
      }
      return input;
   }

   void UpdatePerson()
   {
      string name = "潘佳呈";
      string new_name = "潘佳呈";
      string new_person_id = "HLHF_00005";
      var records = _docModelsService.GetChangePersonRecords(name);
      foreach (var record in records) 
      {
         record.NewPersonName = new_name;
         record.NewPersonId = new_person_id;
         _defaultContext.DocModels.Update(record);
      }
      _defaultContext.SaveChanges();
   }
   void WriteChangePersonRecords()
   {
      string filepath = @"c:\\test\\0913\\set_person.csv";
      var records = _docModelsService.GetChangePersonRecords();
      records = records.OrderBy(x => x.NewPersonId).ToList();
      using (StreamWriter writer = new StreamWriter(filepath, false, Encoding.UTF8))
      {
         var headers = new List<string>() { "舊系統公文文號", "新系統公文文號", "收創日期", "新承辦人姓名", "新承辦人代號", "主旨" };
         writer.WriteLine(headers.JoinToString());
         foreach (var model in records)
         {
            string title = RemoveStartingDoubleQuote(model.Title);
            var cols = new List<string>() { model.Old_Num, model.Num, model.Date, model.NewPersonName!.ToString(), model.NewPersonId!.ToString(), title };
            writer.WriteLine(cols.JoinToString());
         }
      }
   }
   void WriteHideRecords()
   {
      string filepath = @"c:\\test\\0913\\hide.csv";
      var records = _docModelsService.GetHideRecords();
      records = records.OrderBy(x => x.Flag).OrderBy(x => x.Keep).ToList();
      using (StreamWriter writer = new StreamWriter(filepath, false, Encoding.UTF8))
      {
         var headers = new List<string>() { "舊系統公文文號", "新系統公文文號", "收創日期", "承辦人姓名", "備註", "主旨" };
         writer.WriteLine(headers.JoinToString());
         foreach (var model in records)
         {
            string ps = model.Flag == 2 ? "審判公文，隨卷歸檔" : "";
            string title = RemoveStartingDoubleQuote(model.Title);
            var cols = new List<string>() { model.Old_Num, model.Num, model.Date, model.Person, ps, title };
            writer.WriteLine(cols.JoinToString());
         }
      }
   }
   void WriteUpdateStatusRecords()
   {
      var flags = new List<int>() { 1, 3, 4 };
      string filepath = @"c:\\test\\0913\\update_status.csv";
      var records = _docModelsService.GetKeepRecords(new List<int>());
      using (StreamWriter writer = new StreamWriter(filepath, false, Encoding.UTF8))
      {
         var headers = new List<string>() { "舊系統公文文號", "新系統公文文號", "收創日期", "承辦人姓名", "狀態Id", "狀態文字", "主旨" };
         writer.WriteLine(headers.JoinToString());
         foreach (var model in records)
         {
            string title = RemoveStartingDoubleQuote(model.Title);
            var cols = new List<string>() { model.Old_Num, model.Num, model.Date, model.Person, model.Keep.ToString(), StatusText(model.Keep), title };
            writer.WriteLine(cols.JoinToString());
         }
      }
   }



   string StatusText(int status)
   { 
      if(status == 1) return "待歸檔";
      if (status == 3) return "待結案";
      if (status == 4) return "已歸檔";
      return "";
   }
   

   IEnumerable<DocModel> SetNewPersonITRecords(int flag, string person, string newPersonName, string newPersonId)
   {
      string ps = $"此案請轉新承辦人：{newPersonName} (代號{newPersonId})";
      var records = _defaultContext.DocModels.Where(x => x.Flag == flag && x.Person == person.Trim()).ToList();
      foreach (var record in records)
      {
         record.NewPersonId = newPersonId;
         record.NewPersonName = newPersonName;
         record.Ps = ps;
         _defaultContext.DocModels.Update(record);
      }
      _defaultContext.SaveChanges();
      return records;
   }

   void MoveFlag(string person)
   {
      int flag = 0;
      var records = _defaultContext.DocModels.Where(x => x.Flag == flag && x.Person == person.Trim()).ToList();
      foreach (var record in records)
      {
         record.Flag = -1;
         _defaultContext.DocModels.Update(record);
      }
      _defaultContext.SaveChanges();
   }
   void MovePerson()
   {
      int flag = 0;
      var persons = _defaultContext.UnitPersons.Where(x => x.Flag == flag).ToList();
      foreach (var person in persons)
      {
         var count = _defaultContext.DocModels.Where(x => x.Flag == flag && x.Person == person.Person.Trim()).Count();
         if (count < 1)
         {
            person.Flag = -1;
            _defaultContext.UnitPersons.Update(person);
            _defaultContext.SaveChanges();
         }
      }
   }

   void SetYear(DefaultContext context)
   {
      var records = context.DocModels.ToList();
      foreach (var item in records)
      {
         item.Year = GetYearFromDateString(item.Date);
         
         context.DocModels.Update(item);
      }
      context.SaveChanges();
   }
   int GetYearFromDateString(string dateString)
   {
      // Split the string by '/'
      string[] parts = dateString.Split('/');

      // Parse the first part as an integer
      if (int.TryParse(parts[0], out int year))
      {
         return year;
      }

      // Handle the case where parsing fails (return -1 or throw an exception)
      throw new ArgumentException("Invalid date string format.");
   }

   async Task ImportFromCSV()
   {
      string path = @"C:\temp\hlh2.csv";
      var records = ReadCsvFile(path);
      foreach (var record in records)
      {
         await AddDocModelIfNotExist(_defaultContext, record);
      }
      _defaultContext.SaveChanges();
   }

   List<DocModel> ReadCsvFile(string path)
   {
     
      var records = new List<DocModel>();
      //using (var reader = new StreamReader(path, Encoding.UTF8))
      using (var reader = new StreamReader(path))
      {
         // Skip the header line
         var header = reader.ReadLine();
         while (!reader.EndOfStream)
         {
            var line = reader.ReadLine();
            var values = line!.Split(',');

            var record = new DocModel
            {
               Num = values[0].Trim(),
               Old_Num = values[1].Trim(),
               Old_CNum = values[2].Trim(),
               Unit = values[3].Trim(),
               Date = values[4].Trim(),
               Person = values[5].Trim(),
               Result = values[6].Trim(),
               Title = values[7].Trim(),
               Flag = 0
            };

            records.Add(record);
         }
      }
      return records;
   }
   async Task AddDocModelIfNotExist(DefaultContext context, DocModel record)
   {
      var exist = await context.DocModels.FirstOrDefaultAsync(x => x.Num == record.Num);
      if (exist == null) context.DocModels.Add(record);
      else
      {
         record.SetValuesTo(exist);
         context.DocModels.Update(exist);
      }
   }
   void ExportDatabaseToBacpac(string connectionString, string bacpacFilePath)
   {
      try
      {
         // Create an instance of DacServices with the connection string
         DacServices dacServices = new DacServices(connectionString);

         // Subscribe to the Message event to receive status messages
         dacServices.Message += (sender, e) => Console.WriteLine(e.Message);

         Console.WriteLine("Starting export...");

         // Perform the export
         dacServices.ExportBacpac(bacpacFilePath, "hlh_api");

         Console.WriteLine($"Export completed. Bacpac file saved to: {bacpacFilePath}");
      }
      catch (Exception ex)
      {
         Console.WriteLine($"An error occurred: {ex.Message}");
      }
   }
}
