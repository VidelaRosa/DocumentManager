﻿using FileSharing.Services.Dtos;
using FileSharing.Services.Exceptions;
using FileSharingWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace FileSharingWeb.Controllers
{
    public class HomeController : BaseController
    {
        readonly ILogger _logger;
        readonly IStringLocalizer _localizer;

        public HomeController(ILoggerFactory loggerFactory, IStringLocalizerFactory factory)
        {
            _logger = loggerFactory.CreateLogger<PublicController>();
            var type = typeof(Resources);
            _localizer = factory.Create(type);
        }

        [HttpGet]
        public IActionResult Index(int? id)
        {
            List<File> files = new List<File>();
            try
            {
                ViewBag.FolderRootId = id;
                if (id.HasValue)
                {
                    var folder = Services.Folder.Read(SecurityToken, id.Value);
                    if (folder.IdFolderRoot.HasValue)
                    {
                        var folderRoot = Services.Folder.Read(SecurityToken, folder.IdFolderRoot.Value);
                        ViewBag.LinkFolder = folderRoot.Id + "," + folder.Name;
                    }
                    else
                    {
                        ViewBag.LinkFolder = _localizer["ROOT"];
                    }
                }
                var folders = Services.Folder.GetFoldersInFolder(SecurityToken, id);
                if (folders != null && folders.Count > 0)
                {
                    foreach(var folder in folders)
                    {
                        files.Add(new File
                        {
                            Id = folder.Id,
                            Name = folder.Name,
                            Type = FileType.Folder
                        });
                    }
                }
                var documents = Services.Document.GetDocumentsInFolder(SecurityToken, id);
                if (documents != null && documents.Count > 0)
                {
                    foreach(var document in documents)
                    {
                        files.Add(new File
                        {
                            Id = document.Id,
                            Name = document.Filename,
                            Type = FileType.Document
                        });
                    }
                }
            }
            catch (FileSharingException e)
            {
                //TODO Mostrar error
                _logger.LogError(2, e.Message);
            }
            return View(files);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(SecurityToken))
                {
                    Services.User.Logout(SecurityToken);
                    Response.Cookies.Delete("SecurityToken");
                }
            }
            catch (FileSharingException e)
            {
                _logger.LogError(2, e.Message);
            }
            return RedirectToAction("Index", "Public");
        }

        [HttpPost]
        public IActionResult CreateFolder(string folderName, string folderRoot)
        {
            long? idFolder = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(folderRoot))
                {
                    long idFolderRoot;
                    long.TryParse(folderRoot, out idFolderRoot);
                    idFolder = idFolderRoot;
                }
                var folder = new FolderDto
                {
                    Name = folderName,
                    IdFolderRoot = idFolder
                };
                Services.Folder.Create(SecurityToken, folder);
            }
            catch (FileSharingException e)
            {
                _logger.LogError(2, e.Message);
            }
            return RedirectToAction("Index", "Home", new { id = idFolder });
        }

        [HttpGet]
        public IActionResult UploadFile()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}
