using Microsoft.ProjectOxford.Vision;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ImageResizer;
using AIAgain.Models;

namespace AIAgain.Controllers
{
    public class DescribeController : VisionController
    {
        
        public ActionResult Index()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("images");
            List<Blobs> blobs = new List<Blobs>();

            foreach (IListBlobItem item in container.ListBlobs())
            {
                var blob = item as CloudBlockBlob;

                if (blob != null)
                {
                    blobs.Add(new Blobs()
                    {
                        Image = blob.Uri.ToString(),
                        Thumbnail = blob.Uri.ToString().Replace("/images/", "/images/")
                    });
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Index(HttpPostedFileBase file)
        {
            if (Request.HttpMethod == "GET")
            {
                return View("Index");
            }

            var model = new DescribePlease();

         
            var features = new[]
            {
                VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description,
                VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags
            };


           
            await RunOperationOnImage(async stream =>
            {
                model.Result = await VisionServiceClient.AnalyzeImageAsync(stream, features);
            });

          
            await RunOperationOnImage(async stream => {
                var bytes = new byte[stream.Length];

                await stream.ReadAsync(bytes, 0, bytes.Length);

                var base64 = Convert.ToBase64String(bytes);
                model.Store = String.Format("data:image/png;base64,{0}", base64);
            });



            if (file != null && file.ContentLength > 0)
            {
                if (!file.ContentType.StartsWith("image"))
                {
                    TempData["Message"] = "Only image files may be uploaded";
                }
                else
                {
                    try
                    {
                        
                        CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                        CloudBlobClient client = account.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference("images");
                        CloudBlockBlob photo = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                        await photo.UploadFromStreamAsync(file.InputStream);
                        
                        using (var outputStream = new MemoryStream())
                        {
                            file.InputStream.Seek(0L, SeekOrigin.Begin);
                            var settings = new ResizeSettings { MaxWidth = 192 };
                            ImageBuilder.Current.Build(file.InputStream, outputStream, settings);
                            outputStream.Seek(0L, SeekOrigin.Begin);
                            container = client.GetContainerReference("images");
                            CloudBlockBlob thumbnail = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                            await thumbnail.UploadFromStreamAsync(outputStream);

                        }
                        VisionServiceClient vision = new VisionServiceClient(
    ConfigurationManager.AppSettings["CognitiveServicesVisionApiKey"],
    ConfigurationManager.AppSettings["CognitiveServicesVisionApiUrl"]
);

                        VisualFeature[] featuresa = new VisualFeature[] { VisualFeature.Description };
                        var result = await vision.AnalyzeImageAsync(photo.Uri.ToString(), featuresa);


                        photo.Metadata.Add("Caption", result.Description.Captions[0].Text );
                     
                        for (int i = 0; i < result.Description.Tags.Length; i++)
                        {
                            string key = String.Format("Tag{0}", i);
                            photo.Metadata.Add(key, result.Description.Tags[i]);
                        }

                        await photo.SetMetadataAsync();

                      
                    }
                    catch (Exception ex)
                    {
                       
                        TempData["Message"] = ex.Message;
                    }
                }
            } 
            return View(model);
        }
  
        public ActionResult Display()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("images");
            List<Blobs> blobs = new List<Blobs>();

            foreach (IListBlobItem item in container.ListBlobs())
            {
                var blob = item as CloudBlockBlob;

                if (blob != null)
                {

                    blob.FetchAttributes(); 
                    var caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name;
                    var Tager = blob.Metadata.ContainsKey("Tag0") ? blob.Metadata["Tag0"] : blob.Name;
                    var Tager1 = blob.Metadata.ContainsKey("Tag1") ? blob.Metadata["Tag1"] : blob.Name;
                    var Tager2 = blob.Metadata.ContainsKey("Tag2") ? blob.Metadata["Tag2"] : blob.Name;
                    var Tager3= blob.Metadata.ContainsKey("Tag3") ? blob.Metadata["Tag3"] : blob.Name;
                    var Tager4 = blob.Metadata.ContainsKey("Tag4") ? blob.Metadata["Tag4"] : blob.Name;
                    var IMGName = blob.Metadata.ContainsKey("Tag") ? blob.Metadata["Tag"] : blob.Name;

                    caption += "\n" +"Tags: "+ Tager + ","+ Tager1+ "," + Tager2+ ","+ Tager3 + ","+ "("+ IMGName+ ")";


                    blobs.Add(new Blobs()
                    {
                        Image = blob.Uri.ToString(),
                        Thumbnail = blob.Uri.ToString().Replace("/images/", "/images/"),
                        Caption = caption,
                   
                    });
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            return View();
        }
    }
}
