using Catfish.Core.Contexts;
using Catfish.Core.Models;
using Catfish.Core.Models.Data;
using Catfish.Core.Models.Forms;
using Catfish.Core.Services;
using LexigraphicaIngestion.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LexigraphicaIngestion
{
    public class Program
    {
        private static CatfishDbContext mDb;
        public static CatfishDbContext Db
        {
            get
            {
                if(mDb == null)
                {
                    mDb = new CatfishDbContext();
                }

                return mDb;
            }
        }

        private static EntityTypeService mEntityTypeSrv;
        public static EntityTypeService EntityTypeSrv
        {
            get { if (mEntityTypeSrv == null) mEntityTypeSrv = new EntityTypeService(Db); return mEntityTypeSrv; }
        }

        private static MetadataService mMetadataSrv;
        public static MetadataService MetadataSrv
        {
            get { if (mMetadataSrv == null) mMetadataSrv = new MetadataService(Db); return mMetadataSrv; }
        }

        private static ItemService mItemSrv;
        public static ItemService ItemSrv
        {
            get { if (mItemSrv == null) mItemSrv = new ItemService(Db); return mItemSrv; }
        }

        private static object PhotoDictionaryLock = new object();
        protected static Dictionary<int, int> PhotoDictionary = new Dictionary<int, int>();

        public static CFMetadataSet CreateCommentMetadataSet()
        {
            CFMetadataSet metadata = new CFMetadataSet();
            metadata.Name = "Comment";

            List<FormField> fields = new List<FormField>();

            TextField user = new TextField();
            user.Name = "User";
            fields.Add(user);

            TextArea comment = new TextArea();
            comment.Name = "Comment";
            fields.Add(comment);

            metadata.Fields = fields;

            metadata.Serialize();
            metadata = MetadataSrv.UpdateMetadataSet(metadata);
            Db.SaveChanges();

            return metadata;
        }

        public static CFMetadataSet CreatePhotoMetadataSet()
        {
            CFMetadataSet metadata = new CFMetadataSet();
            metadata.Name = "Photo";

            List<FormField> fields = new List<FormField>();

            TextField words = new TextField();
            words.Name = "Words";
            fields.Add(words);

            TextField phrases = new TextField();
            phrases.Name = "Phrases";
            fields.Add(phrases);

            TextField description = new TextField();
            phrases.Name = "Description";
            fields.Add(description);

            metadata.Fields = fields;

            metadata.Serialize();
            metadata = MetadataSrv.UpdateMetadataSet(metadata);
            Db.SaveChanges();

            return metadata;
        }

        private static CFMetadataSet CheckAndCreateMetadataSet(string name, Func<CFMetadataSet> createFunction)
        {
            CFMetadataSet metadata = Db.MetadataSets.ToList().Where(m => m.Name == name).FirstOrDefault();

            if (metadata == null)
            {
                return createFunction();
            }

            return metadata;
        }

        private static CFEntityType CreatePhotoEntityType()
        {
            CFEntityType entityType = new CFEntityType();

            entityType.Name = "Photo";
            entityType.TargetTypes = "Items";

            entityType.MetadataSets.Add(CheckAndCreateMetadataSet("Photo", CreatePhotoMetadataSet));

            entityType.AttributeMappings.Add(new CFEntityTypeAttributeMapping()
            {
                Name = "Name Mapping",
                FieldName = "Words",
                MetadataSet = entityType.MetadataSets.First()
            });

            entityType.AttributeMappings.Add(new CFEntityTypeAttributeMapping()
            {
                Name = "Description Mapping",
                FieldName = "Phrases",
                MetadataSet = entityType.MetadataSets.First()
            });

            EntityTypeSrv.UpdateEntityType(entityType);
            Db.SaveChanges();

            return EntityTypeSrv.GetEntityTypeByName("Photo");
        }

        private static CFEntityType CreateCommentEntityType()
        {
            CFEntityType entityType = new CFEntityType();

            entityType.Name = "Comment";
            entityType.TargetTypes = "Items";

            entityType.MetadataSets.Add(CheckAndCreateMetadataSet("Comment", CreateCommentMetadataSet));

            entityType.AttributeMappings.Add(new CFEntityTypeAttributeMapping()
            {
                Name = "Name Mapping",
                FieldName = "User",
                MetadataSet = entityType.MetadataSets.First()
            });

            entityType.AttributeMappings.Add(new CFEntityTypeAttributeMapping()
            {
                Name = "Description Mapping",
                FieldName = "Comment",
                MetadataSet = entityType.MetadataSets.First()
            });

            EntityTypeSrv.UpdateEntityType(entityType);
            Db.SaveChanges();

            return EntityTypeSrv.GetEntityTypeByName("Comment");
        }

        private static CFEntityType CheckAndCreateEntityType(string name, Func<CFEntityType> createFunction)
        {
            CFEntityType type = EntityTypeSrv.GetEntityTypeByName(name);

            if(type == null)
            {
                return createFunction();
            }

            return type;
        }

        public static int AddComment(Comment comment, User user, IDictionary<int, int> photoMap, int entityTypeId)
        {
            CFItem item = ItemSrv.CreateItem(entityTypeId);

            CFMetadataSet metadata = item.MetadataSets[0];
            List<TextValue> values = new List<TextValue>();
            TextValue value = new TextValue("en", "English", user.username);
            values.Add(value);
            metadata.Fields[0].Values = values;

            values = new List<TextValue>();
            value = new TextValue("en", "English", comment.comment);
            values.Add(value);
            metadata.Fields[0].Values = values;

            item.CreatedByName = user.username;
            item.CreatedByGuid = user.id.ToString();
            item.Updated = DateTime.Parse(comment.updated_at);
            item.Created = DateTime.Parse(comment.created_at);

            ItemSrv.UpdateStoredItem(item);
            Db.SaveChanges();

            // Add the user to the audit log;
            CFAuditEntry entry = new CFAuditEntry(CFAuditEntry.eAction.Create, user == null ? string.Empty : user.username, DateTime.Parse(comment.created_at), new List<CFAuditChangeLog>());
            item.AddAuditEntry(entry);

            item = ItemSrv.UpdateStoredItem(item);
            Db.SaveChanges();

            CFItem photo = ItemSrv.GetItem(photoMap[comment.photo_id]);
            photo.ChildRelations.Add(item);
            Db.Entry(photo).State = System.Data.Entity.EntityState.Modified;
            Db.SaveChanges();

            return item.Id;
        }

        public static void AddToPhotoDictionary(int key, int value)
        {
            lock (PhotoDictionaryLock)
            {
                PhotoDictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Starting point for the application. Requires an argument pointing to the folder path.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // Setup the access context
            AccessContext current = new AccessContext(AccessContext.PublicAccessGuid, true, Db);
            AccessContext.current = current;

            // Setup Solr
            System.Net.ServicePointManager.Expect100Continue = false;
            string solrString = System.Configuration.ConfigurationManager.AppSettings["SolrServer"];
            if (!string.IsNullOrEmpty(solrString))
            {
                SolrService.Init(solrString);
                SolrService.Timeout = 500000;
            }
            else
            {
                Console.Error.WriteLine("Could not initialize connection to the Solr server. Please verify that your SolrServer app property is correct.");
                return;
            }

            // Read the data
            if(args.Length < 1)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }

            string directory = args[0];
            IEnumerable<Comment> comments = Comment.Ingest(Path.Combine(directory, "comments.txt"));
            IEnumerable<Photo> photos = Photo.Ingest(Path.Combine(directory, "photos.txt"));
            IEnumerable<PhotoWord> photoWords = PhotoWord.Ingest(Path.Combine(directory, "photo_words.txt"));
            IEnumerable<Phrase> phrases = Phrase.Ingest(Path.Combine(directory, "phrases.txt"));
            IEnumerable<User> users = User.Ingest(Path.Combine(directory, "users.txt"));
            IEnumerable<Word> words = Word.Ingest(Path.Combine(directory, "words.txt"));

            // Create the Entity Types
            CFEntityType photoEntityType = CheckAndCreateEntityType("Photo", CreatePhotoEntityType);
            CFEntityType commentEntityType = CheckAndCreateEntityType("Comment", CreateCommentEntityType);

            // Add each photo
            int maxAmount = Math.Min(64, photos.Count());
            ManualResetEvent[] handles = new ManualResetEvent[maxAmount];
            int i = 0;

            Action<int, int> addPhoto = (index, handleIndex) =>
            {
                Photo photo = photos.ElementAt(index);
                handles[handleIndex].Reset();
                IEnumerable<Word> inWords = photoWords.Where(pw => pw.photo_id == photo.id).Select(pw => words.Where(w => w.id == pw.word_id).FirstOrDefault());
                IEnumerable<Phrase> inPhrases = phrases.Where(p => p.photo_id == photo.id);
                IEnumerable<User> inUsers = users.Where(u => u.id == photo.user_id);

                PhotoProcessor processor = new PhotoProcessor()
                {
                    AddToDictionary = AddToPhotoDictionary,
                    Photo = photo,
                    Words = inWords,
                    Phrases = inPhrases,
                    Users = inUsers,
                    EntityTypeId = photoEntityType.Id,
                    ImgDirectory = Path.Combine(directory, "pictures"),
                    Handle = handles[handleIndex]
                };

                ThreadPool.QueueUserWorkItem(tp => processor.Execute());
            };

            // Keep adding photo funcitons to the thread pool
            for (i = 0; i < maxAmount; ++i)
            {
                handles[i] = new ManualResetEvent(false);
                addPhoto(i, i);
            }

            while(i < photos.Count())
            {
                if ((i & 0x007F) == 0x007F)
                {
                    Console.WriteLine(string.Format("Photos Completed: {0} / {1}", i, photos.Count()));
                    Console.Out.Flush();
                }

                // Check for any completed handles and trigger them to add a new photo
                int h = WaitHandle.WaitAny(handles);
                addPhoto(i, h);
                ++i;
                //for (int x = h; x < handles.Length; ++x)
                //{
                //    if (handles[x].WaitOne(0))
                //    {
                //        addPhoto(i, x);
                //        ++i;
                //    }
                //}
            }
            WaitHandle.WaitAll(handles);

            // Add each comment
            foreach (Comment comment in comments)
            {
                User user = users.Where(u => u.id == comment.user_id).FirstOrDefault();

                try { 
                    AddComment(comment, user, PhotoDictionary, commentEntityType.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Error adding comment {0}: {1}", comment.id, ex.Message));
                }
            }
        }
    }

    public class PhotoProcessor
    {
        public Action<int, int> AddToDictionary;
        public Photo Photo { get; set; }
        public IEnumerable<Word> Words { get; set; }
        public IEnumerable<Phrase> Phrases { get; set; }
        public IEnumerable<User> Users { get; set; }
        public int EntityTypeId { get; set; }
        public string ImgDirectory { get; set; }
        public ManualResetEvent Handle { get; set; }

        private CatfishDbContext mDb;
        public CatfishDbContext Db
        {
            get
            {
                if (mDb == null)
                {
                    mDb = new CatfishDbContext();
                }

                return mDb;
            }
        }

        private ItemService mItemSrv;
        public ItemService ItemSrv
        {
            get { if (mItemSrv == null) mItemSrv = new ItemService(Db); return mItemSrv; }
        }

        private DataService mDataSrv;
        public DataService DataSrv
        {
            get { if (mDataSrv == null) mDataSrv = new DataService(Db); return mDataSrv; }
        }

        public void Execute()
        {
            try
            {
                AddToDictionary(Photo.id, AddPhoto());
            }catch(Exception ex)
            {
                Console.WriteLine(string.Format("Error adding photo {0} - {1}: {2}", Photo.id, Photo.filename, ex.Message));
            }
            finally
            {
                Handle.Set();
            }
        }

        public int AddPhoto()
        {
            // Create the Metadataset
            CFItem item = ItemSrv.CreateItem(EntityTypeId);
            item.Updated = DateTime.Parse(Photo.updated_at);
            item.Created = DateTime.Parse(Photo.created_at);

            CFMetadataSet metadata = item.MetadataSets[0];

            metadata.Fields[0].SetValues(Words.Select(w => w.word), "en", true);
            metadata.Fields[1].SetValues(Phrases.Select(p => p.phrase), "en", true);

            if (Photo.description == null)
            {
                metadata.Fields[2].SetValues(new string[] { string.Empty }, "en", true);
            }
            else
            {
                metadata.Fields[2].SetValues(new string[] { Photo.description }, "en", true);
            }

            //item = ItemSrv.UpdateStoredItem(item);
            //Db.SaveChanges();

            // Create the images
            string inPath = Path.Combine(ImgDirectory, Photo.filename);
            string outPath = Path.Combine(System.Configuration.ConfigurationManager.AppSettings["UploadRoot"], item.Guid);
            string contentType = System.Web.MimeMapping.GetMimeMapping(inPath);
            using (FileStream fs = File.OpenRead(inPath))
            {
                CFDataFile file = DataSrv.InjestFile(fs, Photo.filename, contentType, outPath);
                item.AttachmentField = new Attachment() { FileGuids = Db.XmlModels.Add(file).Guid };
                Db.SaveChanges();
                //item.AddData(file);
            }

            // Add the user to the audit log
            User creator = Users.FirstOrDefault();
            CFAuditEntry entry = new CFAuditEntry(CFAuditEntry.eAction.Create, creator == null ? string.Empty : creator.username, DateTime.Parse(Photo.created_at), new List<CFAuditChangeLog>());
            item.AddAuditEntry(entry);

            item = ItemSrv.UpdateStoredItem(item);
            Db.SaveChanges();

            return item.Id;
        }
    }
}
