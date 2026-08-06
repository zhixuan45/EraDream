using System.Collections.Generic;

namespace EraDream.Core.Models
{
    // 角色差分姿势/表情定义
    public class CharacterExpression
    {
        public string Name { get; set; } = "default";
        public string ImagePath { get; set; } = "";
    }

    // 角色数据库基础项
    public class CharacterData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string DefaultAvatarPath { get; set; } = "";
        public List<CharacterExpression> Expressions { get; set; } = new List<CharacterExpression>();
    }
}
