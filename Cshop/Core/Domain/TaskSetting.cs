namespace Claw.Modules.Eshop.Domain
{
    public partial class TaskSetting
    {
        public virtual int Id { get; set; }
        public virtual Task Task { get; set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
    }
}