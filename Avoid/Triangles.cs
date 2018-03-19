using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;

namespace Sidestep.Avoid
{

    [Avoider(AvoiderType.Omen, 146)]
    internal class Type_glfan020 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            return AddCone(spellCaster, 20);
        }
    }

    [Avoider(AvoiderType.Omen, 99)]
    [Avoider(AvoiderType.Omen, 105)]
    internal class Type_glfan030 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            return AddCone(spellCaster, 30);
        }
    }

    //gl_fan060_1bf
    [Avoider(AvoiderType.Omen, 3)]
    [Avoider(AvoiderType.Omen, 98)]
    [Avoider(AvoiderType.Omen, 100)]
    [Avoider(AvoiderType.Omen, 159)]
    [Avoider(AvoiderType.Omen, 183)]
    internal class Type_glfan060 : Omen
    {
       
        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            return AddCone(spellCaster, 60);
        }
    }

    [Avoider(AvoiderType.Omen, 4)]
    [Avoider(AvoiderType.Omen, 102)]
    [Avoider(AvoiderType.Omen, 163)]
    [Avoider(AvoiderType.Omen, 184)]
    internal class Type_glfan090 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            return AddCone(spellCaster, 90);
        }
    }
    [Avoider(AvoiderType.Omen, 5)]
    [Avoider(AvoiderType.Omen, 101)]
    [Avoider(AvoiderType.Omen, 120)]
    [Avoider(AvoiderType.Omen, 185)]
    internal class Type_glfan120 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            
            return AddCone(spellCaster, 120);
        }
    }

    [Avoider(AvoiderType.Omen, 28)]
    internal class Type_glfan150 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            
            return AddCone(spellCaster, 150);
        }
    }


    [Avoider(AvoiderType.Omen, 107)]
    internal class Type_glfan180 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            
            return AddCone(spellCaster, 180);
        }
    }

    [Avoider(AvoiderType.Omen, 128)]
    internal class Type_glfan210 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            
            return AddCone(spellCaster, 210);
        }
    }

    [Avoider(AvoiderType.Omen, 15)]
    [Avoider(AvoiderType.Omen, 16)]
    [Avoider(AvoiderType.Omen, 17)]
    internal class Type_glfan270 : Omen
    {

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            
            return AddCone(spellCaster, 270);
        }
    }

    
}