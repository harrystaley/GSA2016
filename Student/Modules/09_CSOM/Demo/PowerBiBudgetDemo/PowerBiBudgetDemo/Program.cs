﻿using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerBiBudgetDemo {

    class Program {

        //static string siteUrl = ConfigurationManager.AppSettings["targetSiteUrl"];
        static string siteUrl = "https://intranet.wingtip.com";
        static ClientContext clientContext = new ClientContext(siteUrl);
        static Site siteCollection = clientContext.Site;
        static Web site = clientContext.Web;
    
        #region "Variables and helper methods for site columns, content types and lists"

        static FieldChoice fldExpenseCategory;
        static FieldDateTime fldExpenseDate;
        static FieldCurrency fldExpenseAmount;


        static FieldText fldExpenseBudgetYear;
        static FieldText fldExpenseBudgetQuarter;
        static FieldCurrency fldExpenseBudgetAmount;

        static ContentType ctypeExpense;
        static ContentType ctypeExpenseBudgetItem;

        static List listExpenses;
        static List listExpenseBudgets;

        static Field CreateSiteColumn(string fieldName, string fieldDisplayName, string fieldType) {

            Console.WriteLine("Creating " + fieldName + " site column...");

            // delete existing field if it exists
            try {
                Field fld = site.Fields.GetByInternalNameOrTitle(fieldName);
                fld.DeleteObject();
                clientContext.ExecuteQuery();
            }
            catch { }

            string fieldXML = @"<Field Name='" + fieldName + "' " +
                                      "DisplayName='" + fieldDisplayName + "' " +
                                      "Type='" + fieldType + "' " +
                                      "Group='Wingtip' > " +
                               "</Field>";

            Field field = site.Fields.AddFieldAsXml(fieldXML, true, AddFieldOptions.DefaultValue);
            clientContext.Load(field);
            clientContext.ExecuteQuery();
            return field;
        }

        static void DeleteContentType(string contentTypeName) {
            try {
                foreach (var ct in site.ContentTypes) {
                    if (ct.Name.Equals(contentTypeName)) {
                        ct.DeleteObject();
                        Console.WriteLine("Deleting existing " + ct.Name + " content type...");
                        clientContext.ExecuteQuery();
                        break;
                    }
                }
            }
            catch { }

        }

        static ContentType CreateContentType(string contentTypeName, string baseContentType) {

            DeleteContentType(contentTypeName);

            ContentTypeCreationInformation contentTypeCreateInfo = new ContentTypeCreationInformation();
            contentTypeCreateInfo.Name = contentTypeName;
            contentTypeCreateInfo.ParentContentType = site.ContentTypes.GetById(baseContentType); ;
            contentTypeCreateInfo.Group = "Wingtip";
            ContentType ctype = site.ContentTypes.Add(contentTypeCreateInfo);
            clientContext.ExecuteQuery();
            return ctype;

        }

        static void DeleteList(string listTitle) {
            try {
                List list = site.Lists.GetByTitle(listTitle);
                list.DeleteObject();
                Console.WriteLine("Deleting existing " + listTitle + " list...");
                clientContext.ExecuteQuery();
            }
            catch { }
        }

        #endregion

        static void Main() {

            clientContext.Load(siteCollection);
            clientContext.Load(site);
            clientContext.Load(site.ContentTypes);
            clientContext.ExecuteQuery();

            DeleteAllCustomTypes();
            CreateSiteColumns();
            CreateContentTypes();

            CreateExpensesList();
            CreateExpenseBudgetsList();

            Console.WriteLine("All done");
        }

        static void DeleteAllCustomTypes() {
            DeleteList("Expenses");
            DeleteList("Expense Budgets");
            DeleteContentType("Expense Item");
            DeleteContentType("Expense Budget Item");
        }

        class ExpenseCategory {
            public const string OfficeSupplies = "Office Supplies";
            public const string Marketing = "Marketing";
            public const string Operations = "Operations";
            public const string ResearchAndDevelopment = "Research & Development";
            public static string[] GetAll() {
                string[] AllCategories = { OfficeSupplies, Marketing, Operations, ResearchAndDevelopment };
                return AllCategories;
            }
        }

        static void CreateSiteColumns() {

            fldExpenseCategory = clientContext.CastTo<FieldChoice>(CreateSiteColumn("ExpenseCategory", "Expense Category", "Choice"));
            string[] choicesExpenseCategory = ExpenseCategory.GetAll();
            fldExpenseCategory.Choices = choicesExpenseCategory;
            fldExpenseCategory.Update();
            clientContext.ExecuteQuery();


            fldExpenseDate = clientContext.CastTo<FieldDateTime>(CreateSiteColumn("ExpenseDate", "Expense Date", "DateTime")); ;
            fldExpenseDate.DisplayFormat = DateTimeFieldFormatType.DateOnly;
            fldExpenseDate.Update();

            fldExpenseAmount = clientContext.CastTo<FieldCurrency>(CreateSiteColumn("ExpenseAmount", "Expense Amount", "Currency"));
            fldExpenseAmount.MinimumValue = 0;

            fldExpenseBudgetYear = clientContext.CastTo<FieldText>(CreateSiteColumn("ExpenseBudgetYear", "Budget Year", "Text"));

            fldExpenseBudgetQuarter = clientContext.CastTo<FieldText>(CreateSiteColumn("ExpenseBudgetQuarter", "Budget Quarter", "Text"));
            fldExpenseBudgetQuarter.Update();

            fldExpenseBudgetAmount = clientContext.CastTo<FieldCurrency>(CreateSiteColumn("ExpenseBudgetAmount", "Budget Amount", "Currency"));
         
            clientContext.ExecuteQuery();
        }

        static void CreateContentTypes() {

            ctypeExpense = CreateContentType("Expense Item", "0x01");
            ctypeExpense.Update(true);
            clientContext.Load(ctypeExpense.FieldLinks);
            clientContext.ExecuteQuery();
            
            FieldLinkCreationInformation fldLinkExpenseCategory = new FieldLinkCreationInformation();
            fldLinkExpenseCategory.Field = fldExpenseCategory;
            ctypeExpense.FieldLinks.Add(fldLinkExpenseCategory);
            ctypeExpense.Update(true);

            // add site columns
            FieldLinkCreationInformation fldLinkExpenseDate = new FieldLinkCreationInformation();
            fldLinkExpenseDate.Field = fldExpenseDate;
            ctypeExpense.FieldLinks.Add(fldLinkExpenseDate);
            ctypeExpense.Update(true);

            // add site columns
            FieldLinkCreationInformation fldLinkExpenseAmount = new FieldLinkCreationInformation();
            fldLinkExpenseAmount.Field = fldExpenseAmount;
            ctypeExpense.FieldLinks.Add(fldLinkExpenseAmount);
            ctypeExpense.Update(true);

            clientContext.ExecuteQuery();

            ctypeExpenseBudgetItem = CreateContentType("Expense Budget Item", "0x01");
            ctypeExpenseBudgetItem.Update(true);
            clientContext.Load(ctypeExpenseBudgetItem.FieldLinks);
            clientContext.ExecuteQuery();

            FieldLinkCreationInformation fldLinkExpenseBudgetCategory = new FieldLinkCreationInformation();
            fldLinkExpenseBudgetCategory.Field = fldExpenseCategory;
            ctypeExpenseBudgetItem.FieldLinks.Add(fldLinkExpenseBudgetCategory);
            ctypeExpenseBudgetItem.Update(true);
            
            FieldLinkCreationInformation fldLinkExpenseBudgetYear = new FieldLinkCreationInformation();
            fldLinkExpenseBudgetYear.Field = fldExpenseBudgetYear;
            ctypeExpenseBudgetItem.FieldLinks.Add(fldLinkExpenseBudgetYear);
            ctypeExpenseBudgetItem.Update(true);

            FieldLinkCreationInformation fldLinkExpenseBudgetQuarter = new FieldLinkCreationInformation();
            fldLinkExpenseBudgetQuarter.Field = fldExpenseBudgetQuarter;
            ctypeExpenseBudgetItem.FieldLinks.Add(fldLinkExpenseBudgetQuarter);
            ctypeExpenseBudgetItem.Update(true);

            FieldLinkCreationInformation fldLinkExpenseBudgetAmount = new FieldLinkCreationInformation();
            fldLinkExpenseBudgetAmount.Field = fldExpenseBudgetAmount;
            ctypeExpenseBudgetItem.FieldLinks.Add(fldLinkExpenseBudgetAmount);
            ctypeExpenseBudgetItem.Update(true);

            clientContext.ExecuteQuery();

        }

        static void CreateExpensesList() {

            string listTitle = "Expenses";
            string listUrl = "Lists/Expenses";

            // delete document library if it already exists
            ExceptionHandlingScope scope = new ExceptionHandlingScope(clientContext);
            using (scope.StartScope()) {
                using (scope.StartTry()) {
                    site.Lists.GetByTitle(listTitle).DeleteObject();
                }
                using (scope.StartCatch()) { }
            }

            ListCreationInformation lci = new ListCreationInformation();
            lci.Title = listTitle;
            lci.Url = listUrl;
            lci.TemplateType = (int)ListTemplateType.GenericList;
            listExpenses = site.Lists.Add(lci);
            listExpenses.OnQuickLaunch = true;
            listExpenses.EnableFolderCreation = false;
            listExpenses.Update();

            
            // attach JSLink script to default view for client-side rendering
            //listExpenses.DefaultView.JSLink = AppRootFolderRelativeUrl + "scripts/CustomersListCSR.js";
            listExpenses.DefaultView.Update();
            listExpenses.Update();
            clientContext.Load(listExpenses);
            clientContext.Load(listExpenses.Fields);
            var titleField = listExpenses.Fields.GetByInternalNameOrTitle("Title");
            titleField.Title = "Expense Description";
            titleField.Update();
            clientContext.ExecuteQuery();

            listExpenses.ContentTypesEnabled = true;
            listExpenses.ContentTypes.AddExistingContentType(ctypeExpense);
            listExpenses.Update();
            clientContext.Load(listExpenses.ContentTypes);
            clientContext.ExecuteQuery();

            ContentType existing = listExpenses.ContentTypes[0];
            existing.DeleteObject();
            clientContext.ExecuteQuery();

            View viewProducts = listExpenses.DefaultView;
            
            viewProducts.ViewFields.Add("ExpenseCategory");
            viewProducts.ViewFields.Add("ExpenseDate");
            viewProducts.ViewFields.Add("ExpenseAmount");
            viewProducts.Update();

            clientContext.ExecuteQuery();
            
            PopulateExpensesList();

        }

        static void CreateExpenseBudgetsList() {

            string listTitle = "Expense Budgets";
            string listUrl = "Lists/ExpenseBudgets";

            // delete document library if it already exists
            ExceptionHandlingScope scope = new ExceptionHandlingScope(clientContext);
            using (scope.StartScope()) {
                using (scope.StartTry()) {
                    site.Lists.GetByTitle(listTitle).DeleteObject();
                }
                using (scope.StartCatch()) { }
            }

            ListCreationInformation lci = new ListCreationInformation();
            lci.Title = listTitle;
            lci.Url = listUrl;
            lci.TemplateType = (int)ListTemplateType.GenericList;
            listExpenseBudgets = site.Lists.Add(lci);
            listExpenseBudgets.OnQuickLaunch = true;
            listExpenseBudgets.EnableFolderCreation = false;
            listExpenseBudgets.Update();

            listExpenseBudgets.DefaultView.Update();
            listExpenseBudgets.Update();
            clientContext.Load(listExpenseBudgets);
            clientContext.Load(listExpenseBudgets.Fields);
            var titleField = listExpenseBudgets.Fields.GetByInternalNameOrTitle("Title");
            titleField.Title = "Expense Budget";
            titleField.Update();
            clientContext.ExecuteQuery();

            listExpenseBudgets.ContentTypesEnabled = true;
            listExpenseBudgets.ContentTypes.AddExistingContentType(ctypeExpenseBudgetItem);
            listExpenseBudgets.Update();
            clientContext.Load(listExpenseBudgets.ContentTypes);
            clientContext.ExecuteQuery();

            ContentType existing = listExpenseBudgets.ContentTypes[0];
            existing.DeleteObject();
            clientContext.ExecuteQuery();

            View viewProducts = listExpenseBudgets.DefaultView;

            viewProducts.ViewFields.Add("ExpenseCategory");
            viewProducts.ViewFields.Add("ExpenseBudgetYear");
            viewProducts.ViewFields.Add("ExpenseBudgetQuarter");
            viewProducts.ViewFields.Add("ExpenseBudgetAmount");
            viewProducts.Update();

            clientContext.ExecuteQuery();

            PopulateExpenseBudgetsList();

        }

        static void AddExpense(string Description, string Category, DateTime Date, decimal Amount) {

            ListItem newItem = listExpenses.AddItem(new ListItemCreationInformation());
            newItem["Title"] = Description;
            newItem["ExpenseCategory"] = Category;
            newItem["ExpenseDate"] = Date;
            newItem["ExpenseAmount"] = Amount;

            newItem.Update();
            clientContext.ExecuteQuery();

            Console.Write(".");
        }

        static void PopulateExpensesList() {

            Console.Write("Adding expenses");

            // January 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 1, 3), 133.44m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 1, 3), 328.40m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 1, 5), 824.90m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 1, 8), 89.40m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 1, 18), 23.90m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 1, 21), 478.33m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 1, 21), 20.00m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 1, 24), 12.50m);
            AddExpense("Toy Stress Tester", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 1, 28), 2400.00m);
            AddExpense("Office Depot supply run", ExpenseCategory.OfficeSupplies, new DateTime(2015, 1, 29), 184.30m);

            // Feb 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 2, 1), 138.02m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 2, 1), 297.47m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 2, 1), 789.77m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 8.95m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 74.55m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 45.67m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 32.34m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 20m);
            AddExpense("Toy Stress Tester", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 2, 1), 2400m);
            AddExpense("Office Depot supply run", ExpenseCategory.OfficeSupplies, new DateTime(2015, 2, 1), 196.44m);
            AddExpense("TV Ads - East Coast", ExpenseCategory.Marketing, new DateTime(2015, 2, 1), 2800m);
            AddExpense("TV Ads - West Coast", ExpenseCategory.Marketing, new DateTime(2015, 2, 1), 2400m);

            // March 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 3, 1), 142.99m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 3, 1), 304.21m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 3, 1), 804.33m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 44.23m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 3, 1), 500m);
            AddExpense("Printer Paper", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 48.20m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 20m);
            AddExpense("Toner Cartridges for Printer", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 220.34m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 8.95m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 3, 1), 12.30m);

            // April 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 4, 1), 138.34m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 4, 1), 344.32m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 4, 1), 812.90m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 4, 1), 32.45m);
            AddExpense("Toy Stress Tester", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 4, 1), 2400m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 4, 1), 500m);
            AddExpense("Print Ad in People Magazine", ExpenseCategory.Marketing, new DateTime(2015, 4, 1), 1200m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 4, 1), 34.20m);
            AddExpense("Toner Cartridges for Printer", ExpenseCategory.OfficeSupplies, new DateTime(2015, 4, 1), 127.88m);


            // May 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 5, 1), 152.55m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 5, 1), 320.45m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 5, 1), 783.44m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 5, 1), 23.90m);
            AddExpense("Toner Cartridges for Printer", ExpenseCategory.OfficeSupplies, new DateTime(2015, 5, 1), 240.50m);
            AddExpense("Printer Paper", ExpenseCategory.OfficeSupplies, new DateTime(2015, 5, 1), 22.32m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 5, 1), 20m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 5, 1), 8.95m);


            // June 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 6, 1), 138.44m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 6, 1), 332.78m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 6, 1), 802.44m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 6, 1), 34.22m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 6, 1), 8.95m);
            AddExpense("Print Ad in People Magazine", ExpenseCategory.Marketing, new DateTime(2015, 6, 1), 1200m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 6, 1), 24.10m);
            AddExpense("Toner Cartridges for Printer", ExpenseCategory.OfficeSupplies, new DateTime(2015, 6, 1), 132.20m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 6, 1), 8.95m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 6, 1), 500m);

            // July 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 7, 1), 135.22m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 7, 1), 333.11m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 7, 1), 798.25m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 7, 1), 8.95m);
            AddExpense("Office Depot supply run", ExpenseCategory.OfficeSupplies, new DateTime(2015, 7, 1), 212.41m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 7, 1), 46.78m);
            AddExpense("Particle Accelerator", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 7, 1), 4800m);

            // August 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 8, 1), 142.20m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 8, 1), 345.80m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 8, 1), 814.87m);
            AddExpense("TV Ads - Southeast", ExpenseCategory.Marketing, new DateTime(2015, 8, 1), 2800m);
            AddExpense("Toy Stress Tester", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 8, 1), 2400m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 8, 1), 8.95m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 8, 1), 500m);
            AddExpense("Server computer", ExpenseCategory.Operations, new DateTime(2015, 8, 1), 2500m);
            AddExpense("Office chairs", ExpenseCategory.OfficeSupplies, new DateTime(2015, 8, 1), 890.10m);


            // September 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 9, 1), 136.10m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 9, 1), 326.01m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 9, 1), 802.90m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 42.34m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 8.95m);
            AddExpense("Printer Paper", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 86.10m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 20m);
            AddExpense("Toner Cartridges for Printer", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 190.50m);
            AddExpense("Paper clips", ExpenseCategory.OfficeSupplies, new DateTime(2015, 9, 1), 8.95m);
            AddExpense("Server computer", ExpenseCategory.Operations, new DateTime(2015, 9, 1), 3200m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 9, 1), 500m);


            // October 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 10, 1), 141.33m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 10, 1), 322.55m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 10, 1), 832.50m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 10, 1), 35.34m);
            AddExpense("TV Ads - Southeast", ExpenseCategory.Marketing, new DateTime(2015, 10, 1), 4800m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 10, 1), 20m);
            AddExpense("Office Depot supply run", ExpenseCategory.OfficeSupplies, new DateTime(2015, 10, 1), 107.33m);
            AddExpense("Server computer", ExpenseCategory.Operations, new DateTime(2015, 10, 1), 2800m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 10, 1), 8.95m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 10, 1), 30.66m);
            AddExpense("Slide rule", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 10, 1), 48.50m);


            // November 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 11, 1), 140.10m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 11, 1), 321.98m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 11, 1), 842.90m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 11, 1), 42.11m);
            AddExpense("TV Ads - West Coast", ExpenseCategory.Marketing, new DateTime(2015, 11, 1), 4800m);
            AddExpense("File cabinet", ExpenseCategory.OfficeSupplies, new DateTime(2015, 11, 1), 120m);
            AddExpense("Printer Paper", ExpenseCategory.OfficeSupplies, new DateTime(2015, 11, 1), 220.34m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 11, 1), 500m);
            AddExpense("Postage Stamps", ExpenseCategory.OfficeSupplies, new DateTime(2015, 11, 1), 20m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 11, 1), 28.35m);

            // December 2015
            AddExpense("Water Bill", ExpenseCategory.Operations, new DateTime(2015, 12, 1), 326.48m);
            AddExpense("Verizon - Telephone Expenses", ExpenseCategory.Operations, new DateTime(2015, 12, 1), 345.32m);
            AddExpense("Electricity Bill", ExpenseCategory.Operations, new DateTime(2015, 12, 1), 840.66m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 8.95m);
            AddExpense("Printer Paper", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 34.20m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 12, 1), 500m);
            AddExpense("Cleaning Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 144.50m);
            AddExpense("Particle Accelerator", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 12, 1), 12000m);
            AddExpense("Pencils", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 8.95m);
            AddExpense("TV Ads - Southeast", ExpenseCategory.Marketing, new DateTime(2015, 12, 1), 3200m);
            AddExpense("Science Calculator", ExpenseCategory.ResearchAndDevelopment, new DateTime(2015, 12, 1), 120m);
            AddExpense("TV Ads - East Coast", ExpenseCategory.Marketing, new DateTime(2015, 12, 1), 2800m);
            AddExpense("TV Ads - West Coast", ExpenseCategory.Marketing, new DateTime(2015, 12, 1), 2400m);
            AddExpense("Coffee Supplies", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 45.33m);
            AddExpense("Google Ad Words", ExpenseCategory.Marketing, new DateTime(2015, 12, 1), 500m);
            AddExpense("Office chairs", ExpenseCategory.OfficeSupplies, new DateTime(2015, 12, 1), 780.32m);
        
            Console.WriteLine();
            Console.WriteLine();
        }

        static void AddExpenseBudget(string Category, string Year, string Quarter, decimal Amount) {

            ListItem newItem = listExpenseBudgets.AddItem(new ListItemCreationInformation());
            newItem["Title"] = Category +" for " + Quarter.ToString() + " of " + Year.ToString();
            newItem["ExpenseCategory"] = Category;
            newItem["ExpenseBudgetYear"] = Year;
            newItem["ExpenseBudgetQuarter"] = Quarter;
            newItem["ExpenseBudgetAmount"] = Amount;

            newItem.Update();
            clientContext.ExecuteQuery();

            Console.Write(".");
        }

        static void PopulateExpenseBudgetsList() {

            Console.Write("Adding expense budgets");

            AddExpenseBudget(ExpenseCategory.OfficeSupplies, "2015", "Q1", 1000m);
            AddExpenseBudget(ExpenseCategory.Marketing, "2015", "Q1", 7500m);
            AddExpenseBudget(ExpenseCategory.Operations, "2015", "Q1", 2000m);
            AddExpenseBudget(ExpenseCategory.ResearchAndDevelopment, "2015", "Q1", 5000m);

            AddExpenseBudget(ExpenseCategory.OfficeSupplies, "2015", "Q2", 1000m);
            AddExpenseBudget(ExpenseCategory.Marketing, "2015", "Q2", 7500m);
            AddExpenseBudget(ExpenseCategory.Operations, "2015", "Q2", 2000m);
            AddExpenseBudget(ExpenseCategory.ResearchAndDevelopment, "2015", "Q2", 5000m);

            AddExpenseBudget(ExpenseCategory.OfficeSupplies, "2015", "Q3", 1000m);
            AddExpenseBudget(ExpenseCategory.Marketing, "2015", "Q3", 10000m);
            AddExpenseBudget(ExpenseCategory.Operations, "2015", "Q3", 2000m);
            AddExpenseBudget(ExpenseCategory.ResearchAndDevelopment, "2015", "Q3", 5000m);

            AddExpenseBudget(ExpenseCategory.OfficeSupplies, "2015", "Q4", 1000m);
            AddExpenseBudget(ExpenseCategory.Marketing, "2015", "Q4", 10000m);
            AddExpenseBudget(ExpenseCategory.Operations, "2015", "Q4", 2000m);
            AddExpenseBudget(ExpenseCategory.ResearchAndDevelopment, "2015", "Q4", 5000m);


            Console.WriteLine();
            Console.WriteLine();
        }



    }
}
