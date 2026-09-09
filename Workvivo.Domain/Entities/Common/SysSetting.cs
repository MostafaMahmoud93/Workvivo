using Workvivo.Domain.Entities.BaseEntities;

namespace Workvivo.Domain.Entities.Common
{
    public class SysSetting : BaseCommonEntity<Guid>
    {
        public int SysSettingID { get; set; }
        public string SysSettingCode { get; set; }
        public string SysSettingDescAr { get; set; }
        public string SysSettingDescEn { get; set; }
        public string SysSettingValue { get; set; }
        public string SysSettingValueDataType { get; set; }
        public virtual MasterData? DataTypeMasterData { get; set; }
    }
}
