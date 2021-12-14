namespace SoftJail.DataProcessor
{

    using Data;
    using Newtonsoft.Json;
    using SoftJail.Data.Models;
    using SoftJail.Data.Models.Enums;
    using SoftJail.DataProcessor.ImportDto;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class Deserializer
    {
        public static string ImportDepartmentsCells(SoftJailDbContext context, string jsonString)
        {
            var sb = new StringBuilder();
            
            var departmentCells = JsonConvert.DeserializeObject<IEnumerable<DepartmentCellInputModel>>(jsonString);
            var departmets = new List<Department>();
            
            foreach (var departmentCell in departmentCells)
            {
                if(!IsValid(departmentCell) || !departmentCell.Cells.All(IsValid) || !departmentCell.Cells.Any())
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }

                var department = new Department
                {
                    Name = departmentCell.Name,
                    Cells = departmentCell.Cells.Select(x => new Cell
                    {
                        CellNumber = x.CellNumber,
                        HasWindow = x.HasWindow
                    })
                    .ToList()
                };
                departmets.Add(department);

                sb.AppendLine($"Imported {department.Name} with {department.Cells.Count} cells");
            }
            context.Departments.AddRange(departmets);
            context.SaveChanges();

            return sb.ToString().TrimEnd();

        }

        public static string ImportPrisonersMails(SoftJailDbContext context, string jsonString)
        {

            var sb = new StringBuilder();

            var prisonerMails = JsonConvert.DeserializeObject<IEnumerable<PresenormailInputModel>>(jsonString);

            var prisioners = new List<Prisoner>();

            foreach (var prisonerMail in prisonerMails)
            {
                if (!IsValid(prisonerMail) || !prisonerMail.Mails.All(IsValid))
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }
                var isValidRelaseDate = DateTime.TryParseExact(prisonerMail.ReleaseDate, "dd/MM/yyyy", 
                    CultureInfo.InvariantCulture,DateTimeStyles.None,out DateTime relaseDate);
                var incarcerationDate = DateTime.ParseExact(prisonerMail.IncarcerationDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                var prisoner = new Prisoner
                {
                    FullName = prisonerMail.FullName,
                    Nickname = prisonerMail.Nickname,
                    Age = prisonerMail.Age,
                    Bail = prisonerMail.Bail,
                    CellId = prisonerMail.CellId,
                    ReleaseDate = isValidRelaseDate ? (DateTime?)relaseDate : null,
                    IncarcerationDate = incarcerationDate,
                    Mails = prisonerMail.Mails.Select(x => new Mail
                    {
                        Description = x.Description,
                        Sender = x.Sender,
                        Address = x.Address

                    })
                    .ToList()
                   
                };
                prisioners.Add(prisoner);

                sb.AppendLine($"Imported {prisoner.FullName} {prisoner.Age} years old");
            }
            context.Prisoners.AddRange(prisioners);
            context.SaveChanges();

            return sb.ToString().TrimEnd();

        }

        public static string ImportOfficersPrisoners(SoftJailDbContext context, string xmlString)
        {
            var sb = new StringBuilder();

            var validOfficers = new List<Officer>();

            var officerPrisoners = XmlConverter.Deserializer<OfficerPrisinorInputModel>(xmlString, "Officers");

            foreach (var officerPrisinor in officerPrisoners)
            {
                if(!IsValid(officerPrisinor))
                {
                    sb.AppendLine("Invalid Data");
                    continue;
                }

                var officer = new Officer
                {
                    FullName = officerPrisinor.Name,
                    Salary = officerPrisinor.Money,
                    DepartmentId = officerPrisinor.DepartmentId,
                    Position = Enum.Parse<Position>(officerPrisinor.Position),
                    Weapon = Enum.Parse<Weapon>(officerPrisinor.Weapon),
                    OfficerPrisoners = officerPrisinor.Prisoners.Select(x => new OfficerPrisoner
                    {
                        PrisonerId = x.Id
                    })
                    .ToList()


                };
                validOfficers.Add(officer);

                sb.AppendLine($"Imported {officer.FullName} ({officer.OfficerPrisoners.Count} prisoners)");
            }
            context.Officers.AddRange(validOfficers);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }
        

        private static bool IsValid(object obj)
        {
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(obj);
            var validationResult = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(obj, validationContext, validationResult, true);
            return isValid;
        }
    }
}