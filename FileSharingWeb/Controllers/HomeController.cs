﻿using FileSharing.Services.Dtos;
using FileSharing.Services.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace FileSharingWeb.Controllers
{
    public class HomeController : BaseController
    {
        readonly ILogger<HomeController> _logger;
        readonly IStringLocalizer _localizer;

        public HomeController(ILogger<HomeController> logger, IStringLocalizerFactory factory)
        {
            _logger = logger;
            var type = typeof(Resources);
            _localizer = factory.Create(type);
        }

        [HttpGet]
        public IActionResult Index(int? id)
        {
            FolderDetailsDto result = new FolderDetailsDto();
            try
            {
                ViewBag.FolderId = id;
                using (var srv = ServicesFactory.Folder)
                    result = srv.GetFolderDetails(SecurityToken, id);
                if (id.HasValue)
                {
                    FolderDto folder;
                    using (var srv = ServicesFactory.Folder)
                        folder = srv.Read(SecurityToken, id.Value);
                    var idRoot = folder.IdFolderRoot;
                    string path = "";
                    while (folder != null)
                    {
                        path = folder.Name + "/" + path;
                        using (var srv = ServicesFactory.Folder)
                            folder = folder.IdFolderRoot.HasValue
                            ? srv.Read(SecurityToken, folder.IdFolderRoot.Value)
                            : null;
                    }
                    ViewBag.LinkFolder = idRoot.HasValue ? idRoot + "," + path : path;
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
            }
            return View(result);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(SecurityToken))
                {
                    using (var srv = ServicesFactory.User)
                       srv.Logout(SecurityToken);
                    Response.Cookies.Delete("SecurityToken");
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
            }
            return RedirectToAction("Index", "Public");
        }

        [HttpPost]
        public IActionResult CreateFolder(string folderName, string folderRoot)
        {
            long? idFolder = null;
            try
            {
                var folder = new FolderDto
                {
                    Name = folderName,
                };
                long idFolderRoot;
                if (long.TryParse(folderRoot, out idFolderRoot))
                {
                    folder.IdFolderRoot = idFolder = idFolderRoot;
                }
                using (var srv = ServicesFactory.Folder)
                    srv.Create(SecurityToken, folder);
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(_localizer[e.Code].Value);
            }
            return Ok();
        }

        [HttpDelete]
        public IActionResult DeleteFolder(string folder)
        {
            try
            {
                long idFolderToDelete;
                if (long.TryParse(folder, out idFolderToDelete))
                {
                    using (var srv = ServicesFactory.Folder)
                    {
                        var idFolder = srv.Read(SecurityToken, idFolderToDelete)?.IdFolderRoot;
                        srv.Delete(SecurityToken, idFolderToDelete);
                    }
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(_localizer[e.Code].Value);
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult UploadFile(long? id)
        {
            try
            {
                if (HttpContext.Request.Form.Files != null
                    && HttpContext.Request.Form.Files.Count > 0
                    && HttpContext.Request.Form.Files[0].Length > 0)
                {
                    var uploadFile = HttpContext.Request.Form.Files[0];
                    var file = new FileDto
                    {
                        Filename = Path.GetFileName(uploadFile.FileName),
                        ContentType = uploadFile.ContentType,
                        IdFolder = id,
                        IsPublic = Convert.ToBoolean(HttpContext.Request.Form["IsPublic"])
                    };
                    long idGroup;
                    if (long.TryParse(HttpContext.Request.Form["IdGroup"], out idGroup)
                        && idGroup > 0)
                    {
                        file.IdGroup = idGroup;
                    }

                    byte[] content;
                    using (var fileStream = uploadFile.OpenReadStream())
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        content = ms.ToArray();
                    }
                    using (var srv = ServicesFactory.File)
                        srv.Create(SecurityToken, file, content);
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(_localizer[e.Code].Value);
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult UpdateFile(long? id, string filename, bool isPublic, long? idGroup)
        {
            FileDto file = new FileDto();
            try
            {
                if (id.HasValue)
                {
                    using (var srv = ServicesFactory.File)
                        file = srv.Read(SecurityToken, id.Value);

                    file.Filename = filename;
                    file.IsPublic = isPublic;
                    file.IdGroup = idGroup == 0? null : idGroup;

                    using (var srv = ServicesFactory.File)
                        srv.Update(SecurityToken, file);
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(_localizer[e.Code].Value);
            }
            return Ok();
        }

        [HttpDelete]
        public IActionResult DeleteFile(long id)
        {
            try
            {
                using (var srv = ServicesFactory.File)
                    srv.Delete(SecurityToken, id);
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
                return BadRequest(_localizer[e.Code].Value);
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult GetFile(long id)
        {
            FileDto result = null;
            try
            {
                using (var srv = ServicesFactory.File)
                    result = srv.Read(SecurityToken, id);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return Json(result);
        }

        [HttpGet]
        public IActionResult GetFileContent(long id)
        {
            byte[] content = null;
            try
            {
                using (var srv = ServicesFactory.File)
                    content = srv.GetFileContent(SecurityToken, id);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return Json(content);
        }

        [HttpGet]
        public FileResult DownloadFile(long id)
        {
            FileDto result = null;
            byte[] content = null;
            try
            {
                using (var srv = ServicesFactory.File)
                {
                    result = srv.Read(SecurityToken, id);
                    content = srv.GetFileContent(SecurityToken, id);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
            }
            return File(content, result.ContentType, result.Filename);
        }

        [HttpGet]
        public IActionResult QueryFiles(string name, int rowQty, int page)
        {
            List<FileDto> result = new List<FileDto>();
            try
            {
                using (var srv = ServicesFactory.File)
                    result = srv.QueryByName(SecurityToken, name, rowQty, page);
            }
            catch (FileSharingException e)
            {
                _logger.LogError(e.Message);
            }
            return Json(result);
        }
    }
}
