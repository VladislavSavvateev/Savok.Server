using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;
using Savok.Server.Abstractions;

namespace Savok.Server.Utils {
	public static class FileCacheManager {
		private static List<CacheItem> Cache { get; } = new();

		public static bool TryToGet(HttpListenerContext context, FileInfo file, out string hash) {
			ClearOldCacheItems();
			var item = Cache.FirstOrDefault(i => i.Path == file.FullName);
			if (item != null) {
				hash = item.Hash;
				return true;
			}

			hash = StoreHash(context, file).Hash;

			return false;
		}

		private static void ClearOldCacheItems() {
			lock (Cache) Cache.RemoveAll(i => DateTime.Now.Subtract(i.StoredAt).TotalMinutes > 30);
		}
		
		private static CacheItem StoreHash(HttpListenerContext context, FileInfo file) {
			string hash; 
	            
			using (var fileStream = file.OpenRead()) {
				using var md5 = MD5.Create();
				hash = md5.ComputeHash(fileStream).Aggregate("", (s, b) => $"{s}{b:x2}");
			}

			var cacheItem = new CacheItem {
				Hash = hash,
				Path = file.FullName
			};
			
			Cache.Add(cacheItem);
			
			return cacheItem;
		}

		private class CacheItem {
			public string Path { get; set; }
			public string Hash { get; set; }
			public DateTime StoredAt { get; set; } = DateTime.Now;
		}
	}
}