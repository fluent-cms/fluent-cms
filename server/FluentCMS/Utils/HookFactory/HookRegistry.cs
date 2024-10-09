namespace FluentCMS.Utils.HookFactory;
public class HookRegistry
{
        public HookList<SchemaPreGetAllArgs> SchemaPreGetAll { get; } = new();
        public HookList<SchemaPostGetOneArgs> SchemaPostGetOne { get; } = new();
        public HookList<SchemaPreSaveArgs> SchemaPreSave { get; } = new();
        public HookList<SchemaPreDelArgs> SchemaPreDel { get; } = new();
        public HookList<QueryPreGetListArgs> QueryPreGetList { get; } = new();
        public HookList<QueryPreGetManyArgs> QueryPreGetMany { get; } = new();
        public HookList<QueryPreGetOneArgs> QueryPreGetOne { get; } = new();
        public HookList<EntityPreGetOneArgs> EntityPreGetOne { get; } = new();
        public HookList<EntityPostGetOneArgs> EntityPostGetOne { get; } = new();
        public HookList<EntityPreGetListArgs> EntityPreGetList { get; } = new();
        public HookList<EntityPostGetListArgs> EntityPostGetList { get; } = new();
        public HookList<EntityPreUpdateArgs> EntityPreUpdate { get; } = new();
        public HookList<EntityPostUpdateArgs> EntityPostUpdate { get; } = new();
        public HookList<EntityPreDelArgs> EntityPreDel { get; } = new();
        public HookList<EntityPostDelArgs> EntityPostDel { get; } = new();
        public HookList<EntityPreAddArgs> EntityPreAdd { get; } = new();
        public HookList<EntityPostAddArgs> EntityPostAdd { get; } = new();
        public HookList<CrosstablePreAddArgs> CrosstablePreAdd { get; } = new();
        public HookList<CrosstablePostAddArgs> CrosstablePostAdd { get; } = new();
        public HookList<CrosstablePreDelArgs> CrosstablePreDel { get; } = new();
        public HookList<CrosstablePostDelArgs> CrosstablePostDel { get; } = new();
}