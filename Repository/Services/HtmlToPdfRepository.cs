
using JSON_To_PDF.Model;
using JSON_To_PDF.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using RazorLight;
using System.IO;
using System.Net;
using static JSON_To_PDF.Response.Result;

using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Html2pdf;
using iText.Html2pdf.Attach.Impl;
using iText.Html2pdf.Css.Apply.Impl;
using iText.StyledXmlParser.Css.Media;
using iText.Kernel.Geom;
using iText.Layout.Font;
using iText.Kernel.Events;
using iText.Kernel.Pdf.Canvas;
using System;

using iText.Layout.Properties;
using iText.IO.Font;
using JSON_To_PDF.Helpers;
using System.Globalization;
using iText.Kernel.Pdf.Canvas.Wmf;
using Org.BouncyCastle.Asn1.Crmf;
using System.Web;
using RestSharp;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using iText.StyledXmlParser.Css.Util;

namespace JSON_To_PDF.Repository.Services
{
    [Internationalization]
    public class HtmlToPdfRepository : IHtmlToPdfRepository
    {
        
        private readonly IRazorLightEngine _razorLightEngine;

        public HtmlToPdfRepository(IRazorLightEngine razorLightEngine)
        {
            _razorLightEngine = razorLightEngine;
        }

        #region main calling unit
        public async Task<ResultResponse> GeneratePdfFromModel(RikiResultSet rikiResult, string language)
        {
            ResultResponse result = new ResultResponse();
            string s = string.Empty;
            try
            {

                if (rikiResult != null)
                {
                    string targetCulture = language;
                    string qbHtml = "QualifiedBorrowerReport.cshtml";
                    string rikiHtml = "RikiReport.cshtml";
                    
                    string fromCulture = "en-EN";
                     targetCulture = language.ToLower();
                    // normalize the culture in case something like en-us was passed 
                    // retrieve only en since Google doesn't support sub-locals

                    string[] tokens = fromCulture.Split('-');

                    if (tokens.Length > 1)
                        fromCulture = tokens[0];
                    // normalize ToCulture
                    tokens = language.Split('-');
                    if (tokens.Length > 1)
                        language = tokens[0];
                    if (language.Trim().ToLower() != "en")
                    {

                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Email[0]))
                        {
                            s = TranslateText(rikiResult.Consumer.Email[0], fromCulture, targetCulture);
                            rikiResult.Consumer.Email[0] = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.FirstName))
                        {
                            s = TranslateText(rikiResult.Consumer.FirstName.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.FirstName = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.LastName))
                        {
                            s = TranslateText(rikiResult.Consumer.LastName.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.LastName = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Address.City))
                        {
                            s = TranslateText(rikiResult.Consumer.Address.City.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.Address.City = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Address.Country))
                        {
                            s = TranslateText(rikiResult.Consumer.Address.Country.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.Address.Country = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Address.Region))
                        {
                            s = TranslateText(rikiResult.Consumer.Address.Region.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.Address.Region = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Address.Street))
                        {
                            s = TranslateText(rikiResult.Consumer.Address.Street.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.Address.Street = s;
                        }
                        if (!string.IsNullOrEmpty(rikiResult.Consumer.Address.Street2))
                        {
                            s = TranslateText(rikiResult.Consumer.Address.Street2.Trim(), fromCulture, targetCulture);
                            rikiResult.Consumer.Address.Street2 = s;
                        }
                        //For loops for Lists 
                        for (int i = 0; i < rikiResult.Remarks.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(rikiResult.Remarks[i].Label))
                            {
                                s = TranslateText(rikiResult.Remarks[i].Label.ToString().Trim(), fromCulture, targetCulture);
                                rikiResult.Remarks[i].Label = s;
                            }
                            if (!string.IsNullOrEmpty(rikiResult.Remarks[i].Message))
                            {
                                s = TranslateText(rikiResult.Remarks[i].Message.ToString().Trim(), fromCulture, targetCulture);
                                rikiResult.Remarks[i].Message = s;
                            }
                        }
                        for (int i = 0; i < rikiResult.GroupedTransactions.Count; i++)
                        {
                            if (!string.IsNullOrEmpty(rikiResult.GroupedTransactions[i].GroupType))
                            {
                                s = TranslateText(rikiResult.GroupedTransactions[i].GroupType.ToString().Trim(), fromCulture, targetCulture);
                                rikiResult.GroupedTransactions[i].GroupType = s;
                            }
                        }
                        //For loops 

                        qbHtml = "QualifiedBorrowerReport." + targetCulture.Trim().ToLower() + ".cshtml";
                        rikiHtml = "RikiReport." + targetCulture.Trim().ToLower() + ".cshtml";

                        String[] File_Paths = new string[] { qbHtml, rikiHtml };

                        foreach (var filepath in File_Paths)
                        {

                            // to read file and return html string

                            string htmlContent = "";

                            using (var reader = new StreamReader(@"Views/" + filepath))
                            {
                                htmlContent = await reader.ReadToEndAsync();
                            }

                            //to read file and return html string

                            string htmlCode = PopulateHtmlWithDynamicValues(htmlContent, rikiResult);
                            var convertedInByte = await ConvertHtmlToPdf(htmlCode);

                            if (convertedInByte != null && convertedInByte.Status && convertedInByte.PdfInByte != null)
                            {
                                result.PdfInByte = convertedInByte.PdfInByte;

                                //to save data to desktop folder directlyy
                                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //get the desktop path
                                string outputPath = System.IO.Path.Combine(desktopPath, "Reports"); // Add subfolder on the desktop
                                string fetchFileNameFromfilepath = System.IO.Path.GetFileNameWithoutExtension(filepath);
                                string outputFilePath = System.IO.Path.Combine(outputPath, fetchFileNameFromfilepath + DateTime.Now.ToString("dd-H.mmtt") + ".pdf");

                                Directory.CreateDirectory(outputPath); // Create the output directory if it doesn't exist
                                                                       //to save data to desktop folder directly

                                File.WriteAllBytes(outputFilePath, convertedInByte.PdfInByte);

                                Console.WriteLine("PDF saved successfully.");

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.Status = false;
            }
            return result;

        }
        #endregion

        #region populate dynamic value to html

        private string PopulateHtmlWithDynamicValues(string htmlContent, RikiResultSet model)
        {
            try
            {
               // var id = model.RikiId;
              
                // Specify the language/locale for localization
                var cultureInfo = new CultureInfo("es-ES"); // Spanish


                string templateKey = "myUniqueTemplateKey" + DateTime.Now.ToString("dd-H:mm:ss:fff.tt");

                var result = _razorLightEngine.CompileRenderStringAsync(templateKey,
                    htmlContent, model).GetAwaiter().GetResult();

                return result;

            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion

        #region convert html code to pdf
            private async Task<ResultResponse> ConvertHtmlToPdf(string htmlCode)
            {
            ResultResponse result = new ResultResponse();
            try
            {

                //byte[] pdfbytes;

                //using (memorystream memorystream = new memorystream())
                //{
                //    converterproperties converterproperties = new converterproperties();
                //    pdfdocument pdfdocument = new pdfdocument(new pdfwriter(memorystream));
                //    pdfdocument.setdefaultpagesize(pagesize.a4);



                //    fontprovider fontprovider = new fontprovider();
                //    fontprovider.addstandardpdffonts();
                //    converterproperties.setfontprovider(fontprovider);
                //    htmlconverter.converttopdf(htmlcode, pdfdocument, converterproperties);
                //    pdfdocument.close(); // close the pdfdocument before converting to a byte array



                //    pdfbytes = memorystream.toarray();
                //}


                //result.pdfinbyte = pdfbytes;
                //result.status = true;
                //return result;


                //header
                //header

                byte[] pdfbytes;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    ConverterProperties converterProperties = new ConverterProperties();
                    PdfDocument pdfDocument = new PdfDocument(new PdfWriter(memoryStream));
                    pdfDocument.SetDefaultPageSize(PageSize.A4);

                    string htmlHeaderContent = "   <table border-spacing=\"0\" cellpadding=\"0\" cellspacing=\"0\" style=\" margin-top: 40px; \r\nfont-family: ' Roboto', sans-serif; " +
                        "background-color: #fff;\">\r\n        " +
                        "<tr>\r\n            " +
                        "<td align=\"justify\">\r\n                " +
                        "<span style=\"background: linear-gradient(90deg, rgba(193, 139, 64, 0) -30%, rgba(193, 57, 19, 1) 27%, rgba(36, 112, 230, 1) 46%, rgba(62, 200, 159, 1) 72%, " +
                        "rgba(47, 237, 237, 1) 100%, rgba(245, 250, 255, 1) 100%);\r\n            height:1px;width:565px;display:block;\"></span>\r\n           " +
                        " </td>\r\n            " +
                        "<td align=\"right\"\r\n                style=\"font-family: 'Roboto', sans-serif;letter-spacing: 3px;font-size:25px;color:#000;font-weight:900;width:150px; " +
                        "padding-left:15px;\">\r\n               <b style=\"font-weight:bold;\">47BILLION</b> \r\n            </td>\r\n   " +
                        "\r\n        </tr>\r\n     </ table >\r\n" +
                        "<!-- Include Google Fonts link for Roboto -->\r\n" +
                        "<link rel=\"stylesheet\" href=\"https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700;900&display=swap\">";
           
                    // Create a custom page event handler for adding headers
                    pdfDocument.AddEventHandler(PdfDocumentEvent.START_PAGE,new CustomHeaderEventHandler(htmlHeaderContent));

                    FontProvider fontProvider = new FontProvider();
                    fontProvider.AddStandardPdfFonts();
                    converterProperties.SetFontProvider(fontProvider);
                    HtmlConverter.ConvertToPdf(htmlCode, pdfDocument, converterProperties);
                    pdfDocument.Close(); // Close the pdfDocument before converting to a byte array

                    pdfbytes = memoryStream.ToArray();
                }

                 result.PdfInByte = pdfbytes;
                result.Status = true;
                return result;

                //header


                //header


            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.Status = false;
            }
            return result;
            }
        #endregion


        #region
        public string TranslateGoogle(string text, string fromCulture, string toCulture)
        {
            //fromCulture = fromCulture.ToLower();
            //toCulture = toCulture.ToLower();

            //// normalize the culture in case something like en-us was passed 
            //// retrieve only en since Google doesn't support sub-locales
            //string[] tokens = fromCulture.Split('-');
            //if (tokens.Length > 1)
            //    fromCulture = tokens[0];

            //// normalize ToCulture
            //tokens = toCulture.Split('-');
            //if (tokens.Length > 1)
            //    toCulture = tokens[0];

            string baseUrl = string.Format(@"http://translate.google.com/translate_a/t?client=j&text={0}&hl=en&sl={1}&tl={2}",
                                       HttpUtility.UrlEncode(text), fromCulture, toCulture);
            var client = new RestClient(baseUrl);
            // Retrieve Translation with HTTP GET call
            //string html = null;
            var request = new RestRequest("", Method.Post);
            request.AddHeader("accept", "application/json");
            request.AddParameter("source", fromCulture);
            request.AddParameter("target", toCulture);
            request.AddParameter("q", text);

            var response = client.Execute(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine(response.Content);
            }
            else
            {
                Console.WriteLine("Error: " + response.ErrorMessage);
            }

            return response.Content;
        }


        static string TranslateText(string text, string fromCulture, string toCulture)
        {
            try
            {
                
                string baseUrl = string.Format(@"http://translate.google.com/translate_a/t?client=j&text={0}&hl=hi&sl={1}&tl={2}",
                HttpUtility.UrlEncode(text), fromCulture, toCulture);
                var client = new RestClient(baseUrl);
                // Retrieve Translation with HTTP GET call
                var request = new RestRequest("", Method.Post);
                request.AddHeader("accept", "application/json");
                request.AddParameter("source", fromCulture);
                request.AddParameter("target", toCulture);
                text = text.Trim().Replace("'\n'", "");
                request.AddParameter("q", text);
                var response = client.Execute(request);
                if (response.IsSuccessful)
                {
                    string res = response.Content.ToString().Remove(0, 2); //Needed to remove unwanted suffix
                    res = res.Remove(res.Length-2, 2); //Needed to remove unwanted prefix
                    return res;
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                return ex.Message.ToString();
            }

        }




        #endregion


    }


public class CustomHeaderEventHandler : IEventHandler
    {
        private string htmlHeader;

        public CustomHeaderEventHandler(string htmlHeader)
        {
            this.htmlHeader = htmlHeader;
        }
        public void HandleEvent(Event currentEvent)
        {
            PdfDocumentEvent docEvent = (PdfDocumentEvent)currentEvent;
            PdfDocument pdfDoc = docEvent.GetDocument();

            int pageNumber = docEvent.GetDocument().GetPageNumber(docEvent.GetPage());

            if (pageNumber >= 2)
            {

                PdfPage page = docEvent.GetPage();

            // Use predefined page size like A4
            PageSize pageSize = PageSize.A4;

            // Create a layout for the header
            float pageWidth = pdfDoc.GetDefaultPageSize().GetWidth();
            float pageHeight = pdfDoc.GetDefaultPageSize().GetHeight();
            float marginLeft = 36; // Adjust this as needed
            float marginRight = 36; // Adjust this as needed
            float marginTop = 36; // Adjust this as needed

            PdfCanvas canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), pdfDoc);


            // Create a div for the header content
            Div headerDiv = new Div()
                .SetWidth(pageWidth - marginLeft - marginRight)
                .SetHorizontalAlignment(HorizontalAlignment.CENTER);

            //// Add your header content here
            //PdfFont font = PdfFontFactory.CreateFont("Helvetica", PdfEncodings.WINANSI, pdfDoc);
            //Paragraph headerText = new Paragraph("Your Header Text")
            //    .SetFont(font)
            //    .SetFontSize(12);

            //// Add the header text to the header div
            //headerDiv.Add(headerText);

            // Convert the HTML string to iText layout elements
            IList<IElement> elements = HtmlConverter.ConvertToElements(htmlHeader);

            // Add the elements to the header div
            foreach (var element in elements)
            {
                headerDiv.Add((IBlockElement)element);
            }

            // Add the header div to the page
            new Canvas(canvas, PageSize.A4, true)
                .Add(headerDiv);
        }
        }
    }

}
