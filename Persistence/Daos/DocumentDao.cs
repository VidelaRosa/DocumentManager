﻿using FileSharing.Persistence.Models;
using System.Collections.Generic;
using System.Linq;

namespace FileSharing.Persistence.Daos
{
    public class DocumentDao : AbstractDao<Document, long>
	{
		public List<Document> GetDocumentsInRoot(long idUser)
		{
			return _dbSet.Where(d => d.IdUser == idUser && d.IdFolder == null).ToList();
		}

        public List<Document> GetDocumentsByIdGroup(long idGroup)
        {
            return _dbSet.Where(d => d.IdGroup == idGroup).ToList();
        }
	}
}
