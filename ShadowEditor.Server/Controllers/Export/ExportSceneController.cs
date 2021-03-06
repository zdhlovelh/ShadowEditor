﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ShadowEditor.Server.Base;
using ShadowEditor.Server.Helpers;
using System.Text;
using ShadowEditor.Server.CustomAttribute;

namespace ShadowEditor.Server.Controllers.Export
{
    /// <summary>
    /// 导出场景控制器
    /// </summary>
    public class ExportSceneController : ApiBase
    {
        /// <summary>
        /// 执行
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [HttpPost]
        [Authority("PUBLISH_SCENE")]
        public JsonResult Run(string ID, int version = -1)
        {
            var mongo = new MongoHelper();

            var filter = Builders<BsonDocument>.Filter.Eq("ID", BsonObjectId.Create(ID));
            var doc = mongo.FindOne(Constant.SceneCollectionName, filter);

            if (doc == null)
            {
                return Json(new
                {
                    Code = 300,
                    Msg = "The scene is not existed!"
                });
            }

            // 获取场景数据
            var collectionName = doc["CollectionName"].AsString;

            List<BsonDocument> docs;

            if (version == -1) // 最新版本
            {
                docs = mongo.FindAll(collectionName).ToList();
            }
            else // 特定版本
            {
                filter = Builders<BsonDocument>.Filter.Eq(Constant.VersionField, BsonInt32.Create(version));
                docs = mongo.FindMany($"{collectionName}{Constant.HistorySuffix}", filter).ToList();
            }

            // 创建临时目录
            var now = DateTime.Now;

            var path = HttpContext.Current.Server.MapPath($"~/temp/{now.ToString("yyyyMMddHHmmss")}");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // 拷贝静态资源
            var viewPath = HttpContext.Current.Server.MapPath("~/view.html");

            var viewFileData = File.ReadAllText(viewPath, Encoding.UTF8);
            viewFileData = viewFileData.Replace("location.origin", "'.'"); // 替换server地址，以便部署到Git Pages上
            viewFileData = viewFileData.Replace("<%SceneID%>", ID); // 发布场景自动把`<%SceneID%>`替换成真实场景ID，不再需要加`SceneID`或`SceneFile`参数
            File.WriteAllText($"{path}/view.html", viewFileData, Encoding.UTF8);

            var faviconPath = HttpContext.Current.Server.MapPath("~/favicon.ico");
            File.Copy(faviconPath, $"{path}/favicon.ico", true);

            var assetsPath = HttpContext.Current.Server.MapPath($"~/assets");
            DirectoryHelper.Copy(assetsPath, $"{path}/assets");

            var buildPath = HttpContext.Current.Server.MapPath($"~/build");
            DirectoryHelper.Copy(buildPath, $"{path}/build");

            // 分析场景，拷贝使用的资源
            var data = new JArray();

            var urls = new List<string>();

            foreach (var i in docs)
            {
                i["_id"] = i["_id"].ToString(); // ObjectId

                var generator = i["metadata"]["generator"].ToString();

                if (generator == "ServerObject") // 服务端模型
                {
                    urls.Add(i["userData"]["Url"].ToString()); // 模型文件

                    if (i["userData"].AsBsonDocument.Contains("Animation")) // MMD模型动画
                    {
                        urls.Add(i["userData"]["Animation"]["Url"].ToString());
                    }
                    if (i["userData"].AsBsonDocument.Contains("CameraAnimation")) // MMD相机动画
                    {
                        urls.Add(i["userData"]["CameraAnimation"]["Url"].ToString());
                    }
                    if (i["userData"].AsBsonDocument.Contains("Audio")) // MMD音频
                    {
                        urls.Add(i["userData"]["Audio"]["Url"].ToString());
                    }
                }
                else if (generator == "SceneSerializer") // 场景
                {
                    if (i["background"].IsBsonDocument) // 贴图或立方体纹理
                    {
                        if (i["background"]["metadata"]["generator"].ToString() == "CubeTextureSerializer") // 立方体纹理
                        {
                            var array = i["background"]["image"].AsBsonArray;
                            foreach (var j in array)
                            {
                                urls.Add(j["src"].ToString());
                            }
                        }
                        else // 普通纹理
                        {
                            urls.Add(i["background"]["image"]["src"].ToString());
                        }
                    }
                }
                else if (generator == "MeshSerializer" || generator == "SpriteSerializer") // 模型
                {
                    if (i["material"].IsBsonArray)
                    {
                        foreach (BsonDocument material in i["material"].AsBsonArray)
                        {
                            GetUrlInMaterial(material, urls);
                        }
                    }
                    else if (i["material"].IsBsonDocument)
                    {
                        GetUrlInMaterial(i["material"].AsBsonDocument, urls);
                    }
                }
                else if (generator == "AudioSerializer")
                {
                    urls.Add(i["userData"]["Url"].ToString());
                }

                data.Add(JsonHelper.ToObject<JObject>(i.ToJson()));
            }

            // 将场景写入文件
            if (!Directory.Exists($"{path}/Scene"))
            {
                Directory.CreateDirectory($"{path}/Scene");
            }

            // 复制资源
            var file = new FileStream($"{path}/Scene/{ID}.txt", FileMode.Create, FileAccess.Write);
            var writer = new StreamWriter(file);
            writer.Write(JsonHelper.ToJson(data));
            writer.Close();
            file.Close();

            foreach (var url in urls)
            {
                if (!url.StartsWith("/")) // 可能是base64地址
                {
                    continue;
                }

                // LOL模型存在多个url，两个url之间用分号分隔
                var _urls = url.Split(';');

                foreach (var _url in _urls)
                {
                    if (string.IsNullOrEmpty(_url))
                    {
                        continue;
                    }

                    var sourceDirName = Path.GetDirectoryName(HttpContext.Current.Server.MapPath($"~{_url}"));
                    var targetDirName = Path.GetDirectoryName($"{path}{_url}");

                    DirectoryHelper.Copy(sourceDirName, targetDirName);
                }
            }

            return Json(new
            {
                Code = 200,
                Msg = "Export successfully!",
                Url = $"/temp/{now.ToString("yyyyMMddHHmmss")}/view.html?sceneFile={ID}"
            });
        }

        /// <summary>
        /// 获取材质中需要下载的URL
        /// </summary>
        /// <param name="material"></param>
        /// <param name="urls"></param>
        public void GetUrlInMaterial(BsonDocument material, List<string> urls)
        {
            if (material["alphaMap"].IsBsonDocument)
            {
                urls.Add(material["alphaMap"]["image"]["src"].ToString());
            }
            else if (material["aoMap"].IsBsonDocument)
            {
                urls.Add(material["aoMap"]["image"]["src"].ToString());
            }
            else if (material["bumpMap"].IsBsonDocument)
            {
                urls.Add(material["bumpMap"]["image"]["src"].ToString());
            }
            else if (material["displacementMap"].IsBsonDocument)
            {
                urls.Add(material["displacementMap"]["image"]["src"].ToString());
            }
            else if (material["emissiveMap"].IsBsonDocument)
            {
                urls.Add(material["emissiveMap"]["image"]["src"].ToString());
            }
            else if (material["envMap"].IsBsonDocument)
            {
                urls.Add(material["envMap"]["image"]["src"].ToString());
            }
            else if (material["lightMap"].IsBsonDocument)
            {
                urls.Add(material["lightMap"]["image"]["src"].ToString());
            }
            else if (material["map"].IsBsonDocument)
            {
                urls.Add(material["map"]["image"]["src"].ToString());
            }
            else if (material["metalnessMap"].IsBsonDocument)
            {
                urls.Add(material["metalnessMap"]["image"]["src"].ToString());
            }
            else if (material["normalMap"].IsBsonDocument)
            {
                urls.Add(material["normalMap"]["image"]["src"].ToString());
            }
            else if (material["roughnessMap"].IsBsonDocument)
            {
                urls.Add(material["roughnessMap"]["image"]["src"].ToString());
            }
        }
    }
}