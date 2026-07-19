// Character Classes and stats logic (Global scope)

const CLASS_TYPES = {
  KNIGHT: 'knight',
  MAGE: 'mage',
  ROGUE: 'rogue',
  PRIEST: 'priest',
  BOMBER: 'bomber',
  SPIRIT: 'spirit',
  ARCHER: 'archer',
  MONK: 'monk'
};

const ACCESSORY_EQUIP_SLOTS = ['accessory', 'accessory2', 'accessory3', 'accessory4'];
const BASE_TURN_MANA_REGEN = 0.075;
const MONSTER_TURN_MANA_REGEN = 0.15;
const MONSTER_SKILL_MANA_COST_MULTIPLIER = 1.35;
const MANA_REGEN_CAP = 0.15;
const MAGIC_BASIC_ATTACK_CLASS_TYPES = [
  CLASS_TYPES.MAGE,
  CLASS_TYPES.PRIEST,
  CLASS_TYPES.BOMBER,
  CLASS_TYPES.SPIRIT
];

function getBasicAttackTypeForClass(classType) {
  return MAGIC_BASIC_ATTACK_CLASS_TYPES.includes(classType) ? 'magic' : 'physical';
}

function getDefaultEquipped() {
  return {
    weapon: null,
    armor: null,
    accessory: null,
    accessory2: null,
    accessory3: null,
    accessory4: null
  };
}

function normalizeEquipped(equipped = {}) {
  return {
    ...getDefaultEquipped(),
    ...(equipped || {})
  };
}

const CLASS_DETAILS = {
  [CLASS_TYPES.KNIGHT]: {
    name: '성기사',
    icon: '🛡️',
    description: '높은 방어력과 생명력으로 전선의 앞을 지키는 든든한 강철 방패입니다. 물리 기술을 사용해 아군을 보호하고 적을 분쇄합니다.',
    baseStats: {
      maxHp: 160,
      maxMp: 50,
      physicalAttack: 18,
      magicAttack: 5,
      defense: 12,
      speed: 8,
      critRate: 0.05
    },
    growth: {
      maxHp: 20,
      maxMp: 5,
      physicalAttack: 3,
      magicAttack: 1,
      defense: 2,
      speed: 0.8,
      critRate: 0.005
    },
    skills: [
      {
        id: 'shield_slam',
        name: '방패 후려치기',
        cost: 15,
        type: 'attack_physical',
        power: 1.3,
        stunChance: 0.5,
        maxStunChance: 1.0,
        description: '방패로 적을 강타하여 공격력의 130% 물리 피해를 주고 강화 시 최대 100% 확률로 1턴간 기절시킵니다.'
      },
      {
        id: 'iron_wall',
        name: '철벽 방어',
        cost: 20,
        type: 'shield',
        power: 0.6, // 60% of Max HP as shield
        description: '최대 체력의 60%만큼 피해를 흡수하는 보호막을 생성합니다. (3턴 지속)'
      },
      {
        id: 'holy_crush',
        name: '성광 분쇄',
        cost: 25,
        type: 'attack_physical_stun',
        power: 1.6,
        stunChance: 0.5,
        maxStunChance: 1.0,
        description: '성스러운 힘으로 적을 강하게 내려쳐 공격력의 160% 물리 피해를 주고 강화 시 최대 100% 확률로 기절시킵니다.'
      }
    ]
  },
  [CLASS_TYPES.MAGE]: {
    name: '원소술사',
    icon: '🔮',
    description: '원소의 신비한 힘을 부려 적에게 파괴적인 마법을 선사합니다. 낮은 생명력을 가졌으나 최강의 화력을 발휘합니다.',
    baseStats: {
      maxHp: 90,
      maxMp: 150,
      physicalAttack: 6,
      magicAttack: 24,
      defense: 4,
      speed: 10,
      critRate: 0.08
    },
    growth: {
      maxHp: 10,
      maxMp: 19,
      physicalAttack: 1,
      magicAttack: 5,
      defense: 0.8,
      speed: 1.2,
      critRate: 0.01
    },
    skills: [
      {
        id: 'fireball',
        name: '화염구',
        cost: 25,
        type: 'attack_magic',
        power: 1.8,
        burnChance: 0.64,
        maxBurnChance: 0.8,
        description: '거대한 화염 구체를 날려 주문력의 180% 마법 피해를 주고 강화 시 최대 80% 확률로 3턴간 화상 상태로 만듭니다.'
      },
      {
        id: 'frost_shield',
        name: '냉기 장벽',
        cost: 20,
        type: 'shield',
        power: 0.55,
        description: '냉기의 장벽을 둘러 최대 체력의 55%만큼 피해를 막는 보호막을 얻습니다.'
      },
      {
        id: 'lightning_storm',
        name: '번개 폭풍',
        cost: 35,
        type: 'attack_magic_crit',
        power: 1.95,
        stunChance: 0.35,
        maxStunChance: 0.7,
        description: '응축된 번개를 퍼부어 주문력의 195% 마법 피해를 주며 치명타 확률이 크게 증가하고 최대 70% 확률로 기절시킵니다.'
      }
    ]
  },
  [CLASS_TYPES.ROGUE]: {
    name: '그림자 자객',
    icon: '🗡️',
    description: '어둠 속에 몸을 숨긴 채 치명적인 급소를 노립니다. 극도로 민첩하며, 빠르고 날카로운 일격을 연달아 날립니다.',
    baseStats: {
      maxHp: 110,
      maxMp: 70,
      physicalAttack: 15,
      magicAttack: 8,
      defense: 6,
      speed: 16,
      critRate: 0.20
    },
    growth: {
      maxHp: 14,
      maxMp: 8,
      physicalAttack: 4,
      magicAttack: 1.5,
      defense: 1.2,
      speed: 1.8,
      critRate: 0.02
    },
    skills: [
      {
        id: 'shadow_strike',
        name: '그림자 습격',
        cost: 20,
        type: 'attack_physical_crit',
        power: 1.82,
        lifeStealRatio: 0.25,
        description: '치명타 확률이 40% 증가한 상태로 공격력의 182% 물리 피해를 주고 피해량의 25%만큼 체력을 회복합니다.'
      },
      {
        id: 'poison_dagger',
        name: '독 묻은 단검',
        cost: 25,
        type: 'attack_physical_poison',
        power: 1.17,
        poisonChance: 1.0,
        maxPoisonChance: 1.0,
        description: '마나 25를 사용해 공격력의 117% 물리 피해를 주고 확정적으로 3턴 동안 맹독 상태로 만듭니다.'
      },
      {
        id: 'twilight_flurry',
        name: '황혼 난무',
        cost: 40,
        type: 'attack_physical_crit_poison',
        power: 2.02,
        forceCrit: true,
        poisonChance: 1.0,
        maxPoisonChance: 1.0,
        description: '마나 40을 사용해 공격력의 202% 물리 피해를 치명타로 적중시키고 확정적으로 중독시킵니다.'
      }
    ]
  },
  [CLASS_TYPES.PRIEST]: {
    name: '빛의 사제',
    icon: '✨',
    description: '빛의 권능으로 회복과 공격을 동시에 수행하는 힐러형 딜러입니다. 실명과 재생으로 긴 전투에 강합니다.',
    baseStats: {
      maxHp: 120,
      maxMp: 130,
      physicalAttack: 7,
      magicAttack: 21,
      defense: 7,
      speed: 10,
      critRate: 0.07
    },
    growth: {
      maxHp: 14,
      maxMp: 16,
      physicalAttack: 1,
      magicAttack: 4.2,
      defense: 1.2,
      speed: 1,
      critRate: 0.008
    },
    skills: [
      {
        id: 'radiant_smite',
        name: '찬란한 징벌',
        cost: 22,
        type: 'attack_magic',
        power: 1.74,
        stunChance: 0.5,
        maxStunChance: 0.75,
        description: '빛으로 적을 징벌해 주문력의 174% 피해를 주고 강화 시 최대 75% 확률로 기절시킵니다.'
      },
      {
        id: 'sanctuary',
        name: '성역',
        cost: 28,
        type: 'shield',
        power: 0.6,
        selfStatus: { type: 'damage_boost', duration: 2, value: 0.2 },
        description: '성역을 펼쳐 최대 체력의 60% 보호막을 얻고 2턴 동안 주는 피해가 20% 증가합니다.'
      },
      {
        id: 'salvation_ray',
        name: '구원의 광선',
        cost: 36,
        type: 'attack_magic',
        power: 1.75,
        lifeStealRatio: 0.2,
        vulnerableChance: 0.19,
        maxVulnerableChance: 0.35,
        description: '주문력의 175% 빛 피해를 주고 피해량 일부를 회복하며 강화 시 최대 35% 확률로 취약을 노립니다.'
      }
    ]
  },
  [CLASS_TYPES.BOMBER]: {
    name: '폭렬술사',
    icon: '💥',
    description: '폭발 마법으로 짧은 시간에 큰 피해를 쏟아붓는 마법 딜러입니다. 화상, 취약, 충격 효과를 활용합니다.',
    baseStats: {
      maxHp: 95,
      maxMp: 135,
      physicalAttack: 5,
      magicAttack: 27,
      defense: 4,
      speed: 9,
      critRate: 0.1
    },
    growth: {
      maxHp: 11,
      maxMp: 17,
      physicalAttack: 0.8,
      magicAttack: 5.3,
      defense: 0.8,
      speed: 0.9,
      critRate: 0.012
    },
    skills: [
      {
        id: 'blast_spark',
        name: '폭염 불꽃',
        cost: 26,
        type: 'attack_magic',
        power: 1.85,
        burnChance: 0.54,
        maxBurnChance: 0.7,
        description: '폭발하는 불꽃으로 주문력의 185% 피해를 주고 강화 시 최대 70% 확률로 화상을 남깁니다.'
      },
      {
        id: 'shatter_bomb',
        name: '파쇄 폭탄',
        cost: 34,
        type: 'attack_magic',
        power: 2.05,
        vulnerableChance: 0.39,
        maxVulnerableChance: 0.55,
        description: '파편 폭발로 주문력의 205% 피해를 주고 강화 시 최대 55% 확률로 취약 상태를 걸어 후속 피해를 키웁니다.'
      },
      {
        id: 'chain_detonation',
        name: '연쇄 기폭',
        cost: 48,
        type: 'attack_magic_crit',
        power: 2.35,
        shockChance: 0.49,
        maxShockChance: 0.65,
        stunChance: 0.09,
        maxStunChance: 0.25,
        description: '연쇄 폭발로 큰 피해를 주고 강화 시 최대 65% 확률의 감전과 최대 25% 확률의 기절을 겁니다.'
      }
    ]
  },
  [CLASS_TYPES.SPIRIT]: {
    name: '정령술사',
    icon: '🌈',
    description: '기본 공격과 스킬을 함께 쓰는 사원소 전문 캐릭터입니다. 불, 물, 바람, 땅 정령을 번갈아 운용합니다.',
    baseStats: {
      maxHp: 105,
      maxMp: 170,
      physicalAttack: 1,
      magicAttack: 22,
      defense: 5,
      speed: 12,
      critRate: 0.06
    },
    growth: {
      maxHp: 12,
      maxMp: 21,
      physicalAttack: 0,
      magicAttack: 4.6,
      defense: 1,
      speed: 1.3,
      critRate: 0.008
    },
    skills: [
      {
        id: 'flame_spirit',
        name: '화염 정령',
        cost: 30,
        type: 'attack_magic',
        power: 1.85,
        burnChance: 0.59,
        maxBurnChance: 0.75,
        description: '불의 정령이 주문력의 155% 피해를 주고 강화 시 최대 75% 확률로 화상을 겁니다.'
      },
      {
        id: 'tide_spirit',
        name: '물결 정령',
        cost: 30,
        type: 'attack_magic',
        power: 1.85,
        selfShieldPower: 0.25,
        description: '물의 정령이 주문력의 185% 피해를 주고 최대 체력의 25% 보호막을 얻습니다.'
      },
      {
        id: 'gale_spirit',
        name: '질풍 정령',
        cost: 30,
        type: 'attack_magic_crit',
        power: 1.85,
        blindChance: 0.5,
        maxBlindChance: 0.75,
        description: '바람의 정령이 치명타 보정 피해를 주고 강화 시 최대 75% 확률로 실명을 겁니다.'
      },
      {
        id: 'earth_spirit',
        name: '대지 정령',
        cost: 30,
        type: 'attack_magic',
        power: 1.85,
        weakenChance: 0.44,
        maxWeakenChance: 0.6,
        vulnerableChance: 0.44,
        maxVulnerableChance: 0.6,
        description: '대지의 정령이 주문력의 185% 피해를 주고 강화 시 최대 60% 확률로 약화와 취약을 겁니다.'
      }
    ]
  },
  [CLASS_TYPES.ARCHER]: {
    name: '바람 궁수',
    icon: '🏹',
    description: '원거리에서 치명타와 상태이상 화살을 쏘는 민첩한 딜러입니다. 출혈, 기절, 회피 운용에 능합니다.',
    baseStats: {
      maxHp: 115,
      maxMp: 85,
      physicalAttack: 17,
      magicAttack: 6,
      defense: 6,
      speed: 15,
      critRate: 0.16
    },
    growth: {
      maxHp: 13,
      maxMp: 9,
      physicalAttack: 4.2,
      magicAttack: 1,
      defense: 1.1,
      speed: 1.7,
      critRate: 0.018
    },
    skills: [
      {
        id: 'aimed_shot',
        name: '정조준 사격',
        cost: 18,
        type: 'attack_physical_crit',
        power: 1.65,
        description: '치명타 확률이 오른 상태로 공격력의 165% 물리 피해를 줍니다.'
      },
      {
        id: 'snare_arrow',
        name: '속박 화살',
        cost: 24,
        type: 'attack_physical',
        power: 1.35,
        stunChance: 0.54,
        maxStunChance: 0.7,
        description: '속박 화살로 피해를 주고 강화 시 최대 70% 확률로 기절을 겁니다.'
      },
      {
        id: 'piercing_volley',
        name: '관통 연사',
        cost: 34,
        type: 'attack_physical',
        power: 1.95,
        bleedChance: 0.49,
        maxBleedChance: 0.65,
        vulnerableChance: 0.19,
        maxVulnerableChance: 0.35,
        description: '관통 화살을 연사해 강화 시 최대 65% 확률의 출혈과 최대 35% 확률의 취약을 노립니다.'
      },
      {
        id: 'windstep_shot',
        name: '바람걸음 사격',
        cost: 30,
        type: 'attack_physical_crit',
        power: 1.55,
        selfStatus: { type: 'evasion_boost', duration: 2, value: 0.18 },
        description: '빠르게 이동하며 사격하고 2턴 동안 회피율을 올립니다.'
      }
    ]
  },
  [CLASS_TYPES.MONK]: {
    name: '무투가',
    icon: '🥋',
    description: '수도승처럼 단련된 육체와 빠른 연격으로 적을 제압하는 물리 치명타 중심 캐릭터입니다.',
    baseStats: {
      maxHp: 130,
      maxMp: 75,
      physicalAttack: 19,
      magicAttack: 4,
      defense: 8,
      speed: 14,
      critRate: 0.18
    },
    growth: {
      maxHp: 15,
      maxMp: 8,
      physicalAttack: 4.4,
      magicAttack: 0.8,
      defense: 1.4,
      speed: 1.6,
      critRate: 0.018
    },
    skills: [
      {
        id: 'iron_fist_combo',
        name: '철권 연타',
        cost: 20,
        type: 'attack_physical_crit',
        power: 1.65,
        description: '치명타 보정이 붙은 연타로 공격력의 165% 물리 피해를 줍니다.'
      },
      {
        id: 'nerve_strike',
        name: '급소 봉쇄',
        cost: 28,
        type: 'attack_physical',
        power: 1.55,
        stunChance: 0.45,
        maxStunChance: 0.7,
        description: '급소를 가격해 공격력의 155% 물리 피해를 주고 강화 시 최대 70% 확률로 기절시킵니다.'
      },
      {
        id: 'inner_focus',
        name: '내공 폭발',
        cost: 36,
        type: 'attack_physical_crit',
        power: 2.05,
        selfStatus: { type: 'damage_boost', duration: 2, value: 0.18 },
        description: '강력한 일격으로 공격력의 205% 물리 피해를 주고 2턴 동안 주는 피해가 18% 증가합니다.'
      }
    ]
  }
};

const SKILL_MANA_INCREASE_PER_LEVEL = 5;
const SKILL_POWER_GROWTH_PER_LEVEL = 0.12;
const SKILL_CHANCE_GROWTH_PER_LEVEL = 0.08;
const SKILL_CHANCE_FIELDS = [
  'stunChance',
  'burnChance',
  'poisonChance',
  'bleedChance',
  'shockChance',
  'freezeChance',
  'blindChance',
  'weakenChance',
  'vulnerableChance',
  'silenceChance',
  'manaBurnChance',
  'regenChance'
];

function getScaledSkill(baseSkill, level = 1) {
  const lv = Math.max(1, level);
  const bonusLevels = lv - 1;
  const scaled = {
    ...baseSkill,
    baseCost: baseSkill.cost,
    basePower: baseSkill.power,
    level: lv,
    cost: baseSkill.cost + bonusLevels * SKILL_MANA_INCREASE_PER_LEVEL
  };

  if (typeof baseSkill.power === 'number') {
    scaled.power = Math.round(baseSkill.power * (1 + bonusLevels * SKILL_POWER_GROWTH_PER_LEVEL) * 100) / 100;
  }

  SKILL_CHANCE_FIELDS.forEach(chanceName => {
    if (typeof baseSkill[chanceName] === 'number') {
      const capName = `max${chanceName[0].toUpperCase()}${chanceName.slice(1)}`;
      const cap = typeof baseSkill[capName] === 'number' ? baseSkill[capName] : 1;
      scaled[chanceName] = Math.min(cap, Math.round((baseSkill[chanceName] + bonusLevels * SKILL_CHANCE_GROWTH_PER_LEVEL) * 100) / 100);
    }
  });

  return scaled;
}

function getSkillUpgradeCost(skillLevel) {
  return Math.round(80 * Math.pow(1.55, Math.max(1, skillLevel) - 1));
}

const STATUS_EFFECT_DEFINITIONS = {
  poison: { name: '중독', icon: '☠️', className: 'status-poison' },
  burn: { name: '화상', icon: '🔥', className: 'status-burn' },
  stun: { name: '기절', icon: '💫', className: 'status-stun' },
  bleed: { name: '출혈', icon: '🩸', className: 'status-bleed' },
  shock: { name: '감전', icon: '⚡', className: 'status-shock' },
  freeze: { name: '빙결', icon: '❄️', className: 'status-freeze' },
  blind: { name: '실명', icon: '🌑', className: 'status-blind' },
  weaken: { name: '약화', icon: '⬇️', className: 'status-weaken' },
  vulnerable: { name: '취약', icon: '🎯', className: 'status-vulnerable' },
  silence: { name: '침묵', icon: '🔇', className: 'status-silence' },
  mana_burn: { name: '마나 연소', icon: '💙', className: 'status-mana-burn' },
  regen: { name: '재생', icon: '🌿', className: 'status-regen' },
  evasion_boost: { name: '회피 상승', icon: '💨', className: 'status-evasion' },
  damage_boost: { name: '피해 증가', icon: '⚔️', className: 'status-damage-boost' }
};

class Fighter {
  constructor(name, type, stats, level = 1) {
    this.name = name;
    this.type = type;
    this.level = level;
    
    // Level scaling stats
    this.maxHp = Math.round(stats.maxHp);
    this.maxMp = Math.round(stats.maxMp);
    this.hp = this.maxHp;
    this.mp = this.maxMp;
    
    this.physicalAttack = Math.round(stats.physicalAttack);
    this.magicAttack = Math.round(stats.magicAttack);
    this.defense = Math.round(stats.defense);
    this.speed = Math.round(stats.speed);
    this.critRate = stats.critRate;
    this.critDamage = stats.critDamage ?? 0.5;
    this.evasion = Math.min(stats.evasion || 0, 0.8);
    this.damageReduction = stats.damageReduction || 0;
    this.lifeSteal = stats.lifeSteal || 0;
    this.manaRegen = stats.manaRegen || 0;
    this.statusPower = stats.statusPower || 0;
    this.itemFind = stats.itemFind || 0;
    
    this.shield = 0;
    this.statusEffects = [];
  }

  takeDamage(amount) {
    let rawDamage = Math.max(0, Math.round(amount));
    const oldHp = this.hp;
    
    // Absorb with shield first
    if (this.shield > 0) {
      if (this.shield >= rawDamage) {
        this.shield -= rawDamage;
        rawDamage = 0;
      } else {
        rawDamage -= this.shield;
        this.shield = 0;
      }
    }

    this.hp = Math.max(0, this.hp - Math.round(rawDamage));
    return oldHp - this.hp;
  }

  heal(amount) {
    const oldHp = this.hp;
    this.hp = Math.min(this.maxHp, this.hp + Math.round(amount));
    return Math.round(this.hp - oldHp);
  }

  restoreMp(amount) {
    const oldMp = this.mp;
    this.mp = Math.min(this.maxMp, this.mp + Math.round(amount));
    return Math.round(this.mp - oldMp);
  }

  addStatusEffect(effectType, duration, value = 0) {
    const existing = this.statusEffects.find(e => e.type === effectType);
    if (existing) {
      existing.duration = duration;
      existing.value = Math.max(existing.value, value);
    } else {
      this.statusEffects.push({ type: effectType, duration, value });
    }
  }

  hasStatusEffect(effectType) {
    return this.statusEffects.some(effect => effect.type === effectType);
  }

  getStatusValue(effectType, fallback = 0) {
    const matching = this.statusEffects.filter(effect => effect.type === effectType);
    if (!matching.length) return fallback;
    return matching.reduce((maxValue, effect) => Math.max(maxValue, effect.value || fallback), fallback);
  }

  getEffectiveDefense() {
    return this.defense;
  }

  getEffectiveEvasion() {
    let effectiveEvasion = this.evasion;
    if (this.hasStatusEffect('evasion_boost')) {
      effectiveEvasion += this.getStatusValue('evasion_boost', 0.15);
    }
    return Math.min(Math.max(effectiveEvasion, 0), 0.8);
  }

  clearStatusEffects() {
    this.statusEffects = [];
    this.shield = 0;
  }

  tickStatusEffects() {
    let dots = { poison: 0, burn: 0, bleed: 0, shock: 0 };
    let hasStun = false;
    let hasFreeze = false;
    let hasSilence = false;
    let regen = 0;
    let manaBurn = 0;

    this.statusEffects.forEach(effect => {
      if (effect.type === 'poison') {
        dots.poison += effect.value;
      } else if (effect.type === 'burn') {
        dots.burn += effect.value;
      } else if (effect.type === 'bleed') {
        dots.bleed += effect.value;
      } else if (effect.type === 'shock') {
        dots.shock += effect.value;
      } else if (effect.type === 'stun') {
        hasStun = true;
      } else if (effect.type === 'freeze') {
        hasFreeze = true;
      } else if (effect.type === 'silence') {
        hasSilence = true;
      } else if (effect.type === 'regen') {
        regen += effect.value;
      } else if (effect.type === 'mana_burn') {
        manaBurn += effect.value;
      }
      effect.duration--;
    });

    const actionAffectingTypes = ['stun', 'freeze', 'blind', 'weaken', 'vulnerable', 'silence', 'evasion_boost', 'damage_boost'];
    this.statusEffects = this.statusEffects.filter(effect => effect.duration > 0 || actionAffectingTypes.includes(effect.type));

    return { dots, hasStun, hasFreeze, hasSilence, regen, manaBurn };
  }

  expireActionStatusEffects() {
    this.statusEffects = this.statusEffects.filter(effect => effect.duration > 0);
  }
}

class PlayerCharacter extends Fighter {
  constructor(name, classType) {
    const classDetail = CLASS_DETAILS[classType];
    super(name, classType, classDetail.baseStats, 1);
    this.classType = classType;
    this.exp = 0;
    this.maxExp = 100;
    this.gold = 100;
    this.skillLevels = {};
    classDetail.skills.forEach(skill => {
      this.skillLevels[skill.id] = 1;
    });
    this.syncSkills();
    
    this.equipped = getDefaultEquipped();
  }

  recalculateStats() {
    this.equipped = normalizeEquipped(this.equipped);

    const detail = CLASS_DETAILS[this.classType];
    const lvMinusOne = this.level - 1;

    this.maxHp = Math.round(detail.baseStats.maxHp + lvMinusOne * detail.growth.maxHp);
    this.maxMp = Math.round(detail.baseStats.maxMp + lvMinusOne * detail.growth.maxMp);
    
    let flatPhysicalAttack = detail.baseStats.physicalAttack + lvMinusOne * detail.growth.physicalAttack;
    let flatMagicAttack = detail.baseStats.magicAttack + lvMinusOne * detail.growth.magicAttack;
    let flatDefense = detail.baseStats.defense + lvMinusOne * detail.growth.defense;
    let flatSpeed = detail.baseStats.speed + lvMinusOne * detail.growth.speed;
    this.critRate = detail.baseStats.critRate + lvMinusOne * detail.growth.critRate;
    this.critDamage = 0.5;
    this.evasion = 0;
    this.damageReduction = 0;
    this.lifeSteal = 0;
    this.manaRegen = 0;
    this.statusPower = 0;
    this.itemFind = 0;

    if (this.equipped.weapon) {
      const weaponStats = this.equipped.weapon.stats;
      flatPhysicalAttack += weaponStats.physicalAttack || 0;
      flatMagicAttack += weaponStats.magicAttack || 0;
      this.maxMp += weaponStats.maxMp || 0;
      this.statusPower += weaponStats.statusPower || 0;
    }
    if (this.equipped.armor) {
      this.maxHp += this.equipped.armor.stats.maxHp || 0;
      flatDefense += this.equipped.armor.stats.defense || 0;
    }
    ACCESSORY_EQUIP_SLOTS.forEach(slotName => {
      const accessory = this.equipped[slotName];
      if (!accessory) return;

      const accessoryStats = accessory.stats;
      this.maxMp += accessoryStats.maxMp || 0;
      flatSpeed += accessoryStats.speed || 0;
      this.critRate += accessoryStats.critRate || 0;
      this.critDamage += accessoryStats.critDamage || 0;
      this.evasion += accessoryStats.evasion || 0;
      this.damageReduction += accessoryStats.damageReduction || 0;
      this.lifeSteal += accessoryStats.lifeSteal || 0;
      this.manaRegen += accessoryStats.manaRegen || 0;
      this.statusPower += accessoryStats.statusPower || 0;
      this.itemFind += accessoryStats.itemFind || 0;
    });

    this.critRate = Math.min(this.critRate, 0.95);
    this.evasion = Math.min(this.evasion, 0.80);
    this.damageReduction = Math.min(this.damageReduction, 0.50);
    this.lifeSteal = Math.min(this.lifeSteal, 0.35);
    this.manaRegen = Math.min(this.manaRegen, MANA_REGEN_CAP);
    this.statusPower = Math.min(this.statusPower, 0.75);
    this.itemFind = Math.min(this.itemFind, 0.50);

    this.physicalAttack = Math.round(flatPhysicalAttack);
    this.magicAttack = Math.round(flatMagicAttack);
    this.defense = Math.round(flatDefense);
    this.speed = Math.round(flatSpeed);

    this.hp = Math.min(this.hp, this.maxHp);
    this.mp = Math.min(this.mp, this.maxMp);
  }

  syncSkills() {
    const detail = CLASS_DETAILS[this.classType];
    this.skillLevels = this.skillLevels || {};
    detail.skills.forEach(skill => {
      if (!this.skillLevels[skill.id]) {
        this.skillLevels[skill.id] = 1;
      }
    });
    this.skills = detail.skills.map(skill => getScaledSkill(skill, this.skillLevels[skill.id]));
  }

  getSkillUpgradeCost(skillId) {
    const level = this.skillLevels?.[skillId] || 1;
    return getSkillUpgradeCost(level);
  }

  upgradeSkill(skillId) {
    if (!this.skillLevels) this.skillLevels = {};
    this.skillLevels[skillId] = (this.skillLevels[skillId] || 1) + 1;
    this.syncSkills();
    return this.skills.find(skill => skill.id === skillId);
  }

  gainExp(amount) {
    let levelsGained = 0;
    this.exp += amount;
    
    while (this.exp >= this.maxExp) {
      this.exp -= this.maxExp;
      this.level++;
      this.maxExp = Math.round(100 * Math.pow(1.3, this.level - 1));
      levelsGained++;
    }

    if (levelsGained > 0) {
      this.recalculateStats();
      this.hp = this.maxHp;
      this.mp = this.maxMp;
    }

    return levelsGained;
  }

  gainGold(amount) {
    this.gold += amount;
    return this.gold;
  }
}

// Bind to window to act as global variables
window.CLASS_TYPES = CLASS_TYPES;
window.ACCESSORY_EQUIP_SLOTS = ACCESSORY_EQUIP_SLOTS;
window.BASE_TURN_MANA_REGEN = BASE_TURN_MANA_REGEN;
window.MONSTER_TURN_MANA_REGEN = MONSTER_TURN_MANA_REGEN;
window.MONSTER_SKILL_MANA_COST_MULTIPLIER = MONSTER_SKILL_MANA_COST_MULTIPLIER;
window.MANA_REGEN_CAP = MANA_REGEN_CAP;
window.MAGIC_BASIC_ATTACK_CLASS_TYPES = MAGIC_BASIC_ATTACK_CLASS_TYPES;
window.getBasicAttackTypeForClass = getBasicAttackTypeForClass;
window.CLASS_DETAILS = CLASS_DETAILS;
window.SKILL_CHANCE_FIELDS = SKILL_CHANCE_FIELDS;
window.STATUS_EFFECT_DEFINITIONS = STATUS_EFFECT_DEFINITIONS;
window.Fighter = Fighter;
window.PlayerCharacter = PlayerCharacter;
window.getDefaultEquipped = getDefaultEquipped;
window.normalizeEquipped = normalizeEquipped;
window.getScaledSkill = getScaledSkill;
window.getSkillUpgradeCost = getSkillUpgradeCost;
