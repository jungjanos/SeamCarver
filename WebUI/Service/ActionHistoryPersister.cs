using Data;
using System;
using System.Threading.Tasks;

namespace WebUI.Service
{
    public class ActionHistoryPersister
    {
        private readonly SeamCarverContext _db;

        public ActionHistoryPersister(SeamCarverContext db)
        {
            _db = db;
        }

        public async Task CreateHistoryEntry(Guid userId, ActionType actionType, string description, params object[] parameters)
        {
            var paramLen = parameters?.Length ?? 0;

            string p0 = 0 < paramLen ? parameters[0]?.ToString() : null;
            string p1 = 1 < paramLen ? parameters[1]?.ToString() : null;
            string p2 = 2 < paramLen ? parameters[2]?.ToString() : null;
            string p3 = 3 < paramLen ? parameters[3]?.ToString() : null;
            string p4 = 4 < paramLen ? parameters[4]?.ToString() : null;

            var action = new UserAction
            {
                UserId = userId,
                ActionType = actionType,
                Descr = description,
                Timestamp = DateTime.Now, 
                Param0 = p0,
                Param1 = p1,
                Param2 = p2,
                Param3 = p3,
                Param4 = p4,
            };

            _db.UserActions.Add(action);
            await _db.SaveChangesAsync();
        }

        public Task CreateHistoryEntry(string userId, ActionType actionType, string description, params object[] parameters)
        {
            if (Guid.TryParse(userId, out Guid userIdGuid))
                return CreateHistoryEntry(userIdGuid, actionType, description, parameters);

            else
                throw new ArgumentException("argument expected in GUID format", nameof(userId));            
        }
    }
}
