// Items Registry, Shop, Inventory, and Blacksmith Logic (Global scope)

const RARITIES = {
  COMMON: { id: 'common', name: '일반', color: '#9ca3af', class: 'rarity-common' },
  UNCOMMON: { id: 'uncommon', name: '고급', color: '#22c55e', class: 'rarity-uncommon' },
  RARE: { id: 'rare', name: '희귀', color: '#3b82f6', class: 'rarity-rare' },
  EPIC: { id: 'epic', name: '영웅', color: '#a855f7', class: 'rarity-epic' },
  LEGENDARY: { id: 'legendary', name: '전설', color: '#f59e0b', class: 'rarity-legendary' },
  MYTHIC: { id: 'mythic', name: '신화', color: '#ec4899', class: 'rarity-mythic' },
  ANCIENT: { id: 'ancient', name: '고대', color: '#14b8a6', class: 'rarity-ancient' },
  IMMORTAL: { id: 'immortal', name: '불멸', color: '#ef4444', class: 'rarity-immortal' },
  RELIC: { id: 'relic', name: '성물', color: '#84cc16', class: 'rarity-relic' },
  CELESTIAL: { id: 'celestial', name: '성좌', color: '#60a5fa', class: 'rarity-celestial' },
  ORIGIN: { id: 'origin', name: '근원', color: '#fb7185', class: 'rarity-origin' },
  TRANSCENDENT: { id: 'transcendent', name: '초월', color: '#f8fafc', class: 'rarity-transcendent' },
  NAMED: { id: 'named', name: '네임드', color: '#facc15', class: 'rarity-named' }
};

const RARITY_MULTS = {
  common: 1.0,
  uncommon: 1.12,
  rare: 1.25,
  epic: 1.6,
  legendary: 2.1,
  mythic: 2.75,
  ancient: 3.5,
  immortal: 4.4,
  relic: 5.2,
  celestial: 6.4,
  origin: 7.8,
  transcendent: 9.2,
  named: 10.5
};

const RARITY_COST_MULTS = {
  common: 1.0,
  uncommon: 1.15,
  rare: 1.3,
  epic: 1.5,
  legendary: 2.0,
  mythic: 2.8,
  ancient: 3.8,
  immortal: 5.2,
  relic: 6.2,
  celestial: 7.4,
  origin: 8.8,
  transcendent: 10.5,
  named: 12.0
};

const RARITY_SELL_VALUES = {
  common: 8,
  uncommon: 14,
  rare: 24,
  epic: 50,
  legendary: 120,
  mythic: 260,
  ancient: 520,
  immortal: 1000,
  relic: 1600,
  celestial: 2600,
  origin: 4200,
  transcendent: 6500,
  named: 9000
};

const NORMAL_RARITY_TABLE = [
  { rarity: RARITIES.COMMON, weight: 45 },
  { rarity: RARITIES.UNCOMMON, weight: 25 },
  { rarity: RARITIES.RARE, weight: 15 },
  { rarity: RARITIES.EPIC, weight: 8 },
  { rarity: RARITIES.LEGENDARY, weight: 4 },
  { rarity: RARITIES.MYTHIC, weight: 2 },
  { rarity: RARITIES.ANCIENT, weight: 0.7 },
  { rarity: RARITIES.IMMORTAL, weight: 0.25 },
  { rarity: RARITIES.RELIC, weight: 0.12 },
  { rarity: RARITIES.CELESTIAL, weight: 0.06 },
  { rarity: RARITIES.ORIGIN, weight: 0.03 }
];

const BOSS_RARITY_TABLE = [
  { rarity: RARITIES.RARE, weight: 35 },
  { rarity: RARITIES.EPIC, weight: 30 },
  { rarity: RARITIES.LEGENDARY, weight: 18 },
  { rarity: RARITIES.MYTHIC, weight: 10 },
  { rarity: RARITIES.ANCIENT, weight: 5 },
  { rarity: RARITIES.IMMORTAL, weight: 1.5 },
  { rarity: RARITIES.RELIC, weight: 0.9 },
  { rarity: RARITIES.CELESTIAL, weight: 0.55 },
  { rarity: RARITIES.ORIGIN, weight: 0.35 },
  { rarity: RARITIES.TRANSCENDENT, weight: 0.2 }
];

const RARITY_ORDER = [
  'common',
  'uncommon',
  'rare',
  'epic',
  'legendary',
  'mythic',
  'ancient',
  'immortal',
  'relic',
  'celestial',
  'origin',
  'transcendent',
  'named'
];

const DUNGEON_RARITY_CAPS = [
  { min: 1, max: 5, rarityId: 'legendary' },
  { min: 6, max: 10, rarityId: 'mythic' },
  { min: 11, max: 15, rarityId: 'ancient' },
  { min: 16, max: 20, rarityId: 'immortal' },
  { min: 21, max: 25, rarityId: 'relic' },
  { min: 26, max: 30, rarityId: 'celestial' },
  { min: 31, max: 35, rarityId: 'origin' },
  { min: 36, max: Infinity, rarityId: 'transcendent' }
];

const NORMAL_DUNGEON_RARITY_POOLS = [
  { min: 1, max: 5, rarityIds: ['common', 'uncommon', 'rare', 'epic'] },
  { min: 6, max: 10, rarityIds: ['rare', 'epic', 'legendary'] },
  { min: 11, max: 15, rarityIds: ['epic', 'legendary', 'mythic'] },
  { min: 16, max: 20, rarityIds: ['legendary', 'mythic', 'ancient'] },
  { min: 21, max: 25, rarityIds: ['mythic', 'ancient', 'immortal'] },
  { min: 26, max: 30, rarityIds: ['ancient', 'immortal', 'relic'] },
  { min: 31, max: 35, rarityIds: ['immortal', 'relic', 'celestial'] },
  { min: 36, max: 40, rarityIds: ['relic', 'celestial', 'origin'] },
  { min: 41, max: Infinity, rarityIds: ['celestial', 'origin'] }
];

const DUNGEON_GEAR_STAT_MULTS = [
  { min: 1, max: 5, multiplier: 1.0 },
  { min: 6, max: 10, multiplier: 1.15 },
  { min: 11, max: 15, multiplier: 1.35 },
  { min: 16, max: 20, multiplier: 1.6 },
  { min: 21, max: 25, multiplier: 1.9 },
  { min: 26, max: 30, multiplier: 2.25 },
  { min: 31, max: 35, multiplier: 2.65 },
  { min: 36, max: 40, multiplier: 3.1 },
  { min: 41, max: Infinity, multiplier: 3.6 }
];

const ITEM_TYPES = {
  WEAPON: 'weapon',
  ARMOR: 'armor',
  ACCESSORY: 'accessory',
  POTION: 'potion'
};

const SHOP_ITEMS = [];

const CRAFTING_RARITY_UPGRADES = [
  ['common', 'uncommon', 40],
  ['uncommon', 'rare', 80],
  ['rare', 'epic', 150],
  ['epic', 'legendary', 280],
  ['legendary', 'mythic', 520],
  ['mythic', 'ancient', 900],
  ['ancient', 'immortal', 1500],
  ['immortal', 'relic', 2400],
  ['relic', 'celestial', 3600],
  ['celestial', 'origin', 5200],
  ['origin', 'transcendent', 7600]
].map(([from, to, cost]) => ({ from, to, cost }));

const NAMED_BOSS_DUNGEONS = {
  5: { name: '제피로스의 폭풍핵', type: ITEM_TYPES.ACCESSORY, stats: { speed: 4, critRate: 0.05, evasion: 0.04 } },
  10: { name: '아트라스 균열검', type: ITEM_TYPES.WEAPON, stats: { physicalAttack: 12, magicAttack: 12, statusPower: 0.04 } },
  15: { name: '루미나 오로라 성배', type: ITEM_TYPES.ACCESSORY, stats: { maxMp: 25, manaRegen: 0.04, damageReduction: 0.04 } },
  20: { name: '제네시온 왕좌갑', type: ITEM_TYPES.ARMOR, stats: { defense: 7, maxHp: 45 } },
  25: { name: '이그라스 잿폭풍 인장', type: ITEM_TYPES.ACCESSORY, stats: { critDamage: 0.12, damageReduction: 0.05, lifeSteal: 0.04 } },
  30: { name: '아르카이온 심장 파편', type: ITEM_TYPES.ACCESSORY, stats: { maxMp: 45, statusPower: 0.1, itemFind: 0.08 } },
  35: { name: '월식 군주의 결속구', type: ITEM_TYPES.WEAPON, stats: { physicalAttack: 18, magicAttack: 18, statusPower: 0.08 } },
  40: { name: '무한성좌의 예복', type: ITEM_TYPES.ARMOR, stats: { defense: 10, maxHp: 60 } },
  45: { name: '근원의 왕관', type: ITEM_TYPES.ACCESSORY, stats: { critRate: 0.1, critDamage: 0.25, statusPower: 0.15, maxMp: 80 } }
};

const WEAPON_PREFIXES = ['연습용', '강화된', '철제', '공허의', '성스러운', '학살자의', '심연의'];
const WEAPON_BASES = {
  knight: ['단검', '검', '기사검', '대검', '전투도끼'],
  mage: ['나뭇가지', '지팡이', '원소 스태프', '룬 완드', '마도서'],
  rogue: ['녹슨 단검', '쌍단검', '비수', '그림자 펜촉', '암살검'],
  priest: ['성서', '빛 지팡이', '축복의 홀', '태양 성물', '성광 완드'],
  bomber: ['폭발 촉매', '화약 지팡이', '기폭 완드', '용암 마도서', '섬광 구체'],
  spirit: ['정령 구슬', '사원소 토템', '바람 부적', '대지 지팡이', '원초의 가지'],
  archer: ['단궁', '장궁', '합성궁', '바람 활', '관통 쇠뇌'],
  monk: ['수련 장갑', '철권갑', '염주 너클', '용문 권갑', '금강 장갑']
};

const ARMOR_PREFIXES = ['낡은', '가죽', '철제', '사슬', '미스릴', '아다만티움', '공허 가죽'];
const ARMOR_BASES = {
  knight: ['판금 갑옷', '기사 갑옷', '강철 중갑', '성기사의 흉갑'],
  mage: ['천 옷', '마법사 로브', '룬 로브', '현자의 예복'],
  rogue: ['가죽 조끼', '그림자 슈트', '어둠의 망토', '밤의 예복'],
  priest: ['사제 예복', '빛의 로브', '성직자 망토', '축복의 성의'],
  bomber: ['화염 로브', '폭발 방호복', '용암 망토', '기폭술사 코트'],
  spirit: ['정령 예복', '사원소 망토', '바람결 로브', '대지 의복'],
  archer: ['사냥꾼 조끼', '궁수 튜닉', '바람 망토', '매의 갑옷'],
  monk: ['수련복', '무투 도복', '금강 조끼', '선승의 법의']
};

const ACCESSORY_PREFIXES = ['낡은', '빛나는', '룬이 새겨진', '별빛의', '공허의', '축복받은', '심연의'];
const ACCESSORY_BASES = [
  { name: '마력 반지', stats: ['maxMp', 'critRate'] },
  { name: '현자의 목걸이', stats: ['maxMp', 'manaRegen'] },
  { name: '칼날 귀걸이', stats: ['critRate', 'critDamage'] },
  { name: '수호 부적', stats: ['damageReduction', 'evasion'] },
  { name: '탐험가 문장', stats: ['itemFind', 'speed'] },
  { name: '흡혈 팔찌', stats: ['lifeSteal', 'critDamage'] },
  { name: '바람 허리띠', stats: ['speed', 'evasion'] },
  { name: '저주 룬석', stats: ['statusPower', 'maxMp'] },
  { name: '전술 브로치', stats: ['critRate', 'damageReduction'] },
  { name: '비전 성배', stats: ['manaRegen', 'statusPower'] },
  { name: '밤안개 망토 장식', stats: ['evasion', 'lifeSteal'] },
  { name: '차원 시계', stats: ['speed', 'critDamage'] }
];

const RATE_STATS = ['critRate', 'critDamage', 'evasion', 'damageReduction', 'lifeSteal', 'manaRegen', 'statusPower', 'itemFind'];
const CLASS_ACCESSORY_PREFERENCES = {
  knight: ['lifeSteal', 'damageReduction'],
  mage: ['manaRegen', 'maxMp'],
  rogue: ['evasion', 'statusPower'],
  priest: ['manaRegen', 'lifeSteal'],
  bomber: ['critDamage', 'statusPower'],
  spirit: ['manaRegen', 'maxMp'],
  archer: ['critRate', 'critDamage'],
  monk: ['critRate', 'critDamage']
};
const GEAR_STAT_LABELS = {
  physicalAttack: '물리 공격력',
  magicAttack: '마법 공격력',
  defense: '방어력',
  maxHp: '최대 체력',
  maxMp: '최대 마나',
  speed: '속도',
  critRate: '치명타율',
  critDamage: '치명타 피해',
  evasion: '회피율',
  damageReduction: '피해 감소',
  lifeSteal: '흡혈',
  manaRegen: '마나 회복',
  statusPower: '상태이상 위력',
  itemFind: '장비 발견'
};

function isRateGearStat(statName) {
  return RATE_STATS.includes(statName);
}

function getGearStatCap(statName) {
  if (statName === 'manaRegen') {
    return typeof window !== 'undefined' ? (window.MANA_REGEN_CAP ?? 0.15) : 0.15;
  }
  return null;
}

function clampGearStatValue(statName, value) {
  const cap = getGearStatCap(statName);
  if (typeof cap === 'number' && typeof value === 'number') {
    return Math.min(value, cap);
  }
  return value;
}

function formatGearStatValue(statName, value) {
  const displayValue = clampGearStatValue(statName, value);
  return isRateGearStat(statName) ? `${Math.round(displayValue * 100)}%` : displayValue;
}

function getGearStatLabel(statName) {
  return GEAR_STAT_LABELS[statName] || statName;
}

function pickWeighted(items, weightFn) {
  const weightedItems = items
    .map(item => ({
      item,
      weight: Math.max(0, weightFn(item))
    }))
    .filter(entry => entry.weight > 0);
  if (!weightedItems.length) return items[0];
  const totalWeight = weightedItems.reduce((total, entry) => total + entry.weight, 0);
  let roll = Math.random() * totalWeight;

  for (const entry of weightedItems) {
    roll -= entry.weight;
    if (roll <= 0) return entry.item;
  }

  return weightedItems[weightedItems.length - 1].item;
}

function getRarityRank(rarityOrId) {
  const rarityId = typeof rarityOrId === 'string' ? rarityOrId : rarityOrId?.id;
  const rank = RARITY_ORDER.indexOf(rarityId);
  return rank === -1 ? RARITY_ORDER.length - 1 : rank;
}

function getMaxRarityIdForDungeon(dungeonNumber) {
  if (!Number.isFinite(dungeonNumber)) return 'transcendent';
  const cap = DUNGEON_RARITY_CAPS.find(entry => dungeonNumber >= entry.min && dungeonNumber <= entry.max);
  return cap?.rarityId || 'transcendent';
}

function filterRarityTableByDungeon(table, dungeonNumber) {
  const maxRank = getRarityRank(getMaxRarityIdForDungeon(dungeonNumber));
  const filtered = table.filter(entry => getRarityRank(entry.rarity) <= maxRank);
  return filtered.length ? filtered : table;
}

function getNormalRarityIdsForDungeon(dungeonNumber) {
  if (!Number.isFinite(dungeonNumber)) return NORMAL_DUNGEON_RARITY_POOLS[NORMAL_DUNGEON_RARITY_POOLS.length - 1].rarityIds;
  const pool = NORMAL_DUNGEON_RARITY_POOLS.find(entry => dungeonNumber >= entry.min && dungeonNumber <= entry.max);
  return pool?.rarityIds || NORMAL_DUNGEON_RARITY_POOLS[NORMAL_DUNGEON_RARITY_POOLS.length - 1].rarityIds;
}

function filterNormalRarityTableByDungeon(dungeonNumber) {
  const allowedIds = getNormalRarityIdsForDungeon(dungeonNumber);
  const filtered = NORMAL_RARITY_TABLE.filter(entry => allowedIds.includes(entry.rarity.id));
  return filtered.length ? filtered : NORMAL_RARITY_TABLE.filter(entry => entry.rarity.id !== 'transcendent');
}

function getDungeonGearStatMultiplier(dungeonNumber) {
  if (!Number.isFinite(dungeonNumber)) return 1;
  const band = DUNGEON_GEAR_STAT_MULTS.find(entry => dungeonNumber >= entry.min && dungeonNumber <= entry.max);
  return band?.multiplier || 1;
}

function pickRarityFromTable(table, dungeonNumber = null) {
  const availableTable = filterRarityTableByDungeon(table, dungeonNumber);
  return pickWeighted(availableTable, entry => entry.weight).rarity;
}

function getRandomNormalRarity(dungeonNumber = null) {
  return pickWeighted(filterNormalRarityTableByDungeon(dungeonNumber), entry => entry.weight).rarity;
}

function getRandomBossRarity(dungeonNumber = null) {
  return pickRarityFromTable(BOSS_RARITY_TABLE, dungeonNumber);
}

function getRarityMultiplier(rarity) {
  return RARITY_MULTS[rarity?.id] || RARITY_MULTS.common;
}

function getRarityCostMultiplier(rarity) {
  return RARITY_COST_MULTS[rarity?.id] || RARITY_COST_MULTS.common;
}

function getRarityBonusOptionCount(rarity) {
  const rarityId = rarity?.id;
  if (rarityId === 'rare' || rarityId === 'epic' || rarityId === 'legendary' || rarityId === 'mythic') return 1;
  if (rarityId === 'ancient' || rarityId === 'immortal' || rarityId === 'relic') return 2;
  if (rarityId === 'celestial' || rarityId === 'origin' || rarityId === 'transcendent' || rarityId === 'named') return 3;
  return 0;
}

function getGearSellValue(item) {
  if (item.type === ITEM_TYPES.POTION) return 5;
  return RARITY_SELL_VALUES[item.rarity?.id] || RARITY_SELL_VALUES.common;
}

function getRarityById(rarityId) {
  return Object.values(RARITIES).find(rarity => rarity.id === rarityId) || RARITIES.COMMON;
}

function getCraftingRecipeData(inventory) {
  return CRAFTING_RARITY_UPGRADES.map(recipe => {
    const ingredients = inventory.filter(item =>
      item.rarity?.id === recipe.from &&
      item.type !== ITEM_TYPES.POTION &&
      item.rarity?.id !== 'named'
    );
    return {
      ...recipe,
      fromRarity: getRarityById(recipe.from),
      toRarity: getRarityById(recipe.to),
      count: ingredients.length,
      ready: ingredients.length >= 3
    };
  });
}

function getPreferredAccessoryStats(classType) {
  return CLASS_ACCESSORY_PREFERENCES[classType] || [];
}

function pickAccessoryBase(classType) {
  const preferredStats = getPreferredAccessoryStats(classType);
  return pickWeighted(ACCESSORY_BASES, accessoryBase => {
    const matches = accessoryBase.stats.filter(statName => preferredStats.includes(statName)).length;
    return 1 + matches * 4;
  });
}

function pickAccessoryBonusStat(statPool, classType) {
  const preferredStats = getPreferredAccessoryStats(classType);
  return pickWeighted(statPool, statName => preferredStats.includes(statName) ? 5 : 1);
}

function addAccessoryStat(stats, statName, mult, rarityMult, dungeonStatMult = 1) {
  const rate = (value) => Math.round(value * 100) / 100;

  if (statName === 'maxMp') stats.maxMp = (stats.maxMp || 0) + Math.round(18 * mult);
  if (statName === 'speed') stats.speed = (stats.speed || 0) + Math.max(1, Math.round(2 * mult));
  if (statName === 'critRate') stats.critRate = (stats.critRate || 0) + rate((0.02 + rarityMult * 0.012) * dungeonStatMult);
  if (statName === 'critDamage') stats.critDamage = (stats.critDamage || 0) + rate((0.08 + rarityMult * 0.03) * dungeonStatMult);
  if (statName === 'evasion') stats.evasion = (stats.evasion || 0) + rate((0.03 + rarityMult * 0.01) * dungeonStatMult);
  if (statName === 'damageReduction') stats.damageReduction = (stats.damageReduction || 0) + rate((0.03 + rarityMult * 0.01) * dungeonStatMult);
  if (statName === 'lifeSteal') stats.lifeSteal = (stats.lifeSteal || 0) + rate((0.03 + rarityMult * 0.008) * dungeonStatMult);
  if (statName === 'manaRegen') stats.manaRegen = clampGearStatValue('manaRegen', (stats.manaRegen || 0) + rate((0.015 + rarityMult * 0.004) * dungeonStatMult));
  if (statName === 'statusPower') stats.statusPower = (stats.statusPower || 0) + rate((0.08 + rarityMult * 0.03) * dungeonStatMult);
  if (statName === 'itemFind') stats.itemFind = (stats.itemFind || 0) + rate((0.05 + rarityMult * 0.02) * dungeonStatMult);
}

function generateRandomGear(classType, level, forceRarity = null, dungeonNumber = null) {
  const typeRoll = Math.random();
  const type = typeRoll < 0.34 ? ITEM_TYPES.WEAPON : typeRoll < 0.67 ? ITEM_TYPES.ARMOR : ITEM_TYPES.ACCESSORY;
  
  const rarity = forceRarity || getRandomNormalRarity(dungeonNumber);
  const rarityMult = getRarityMultiplier(rarity);
  const dungeonStatMult = getDungeonGearStatMultiplier(dungeonNumber);
  const mult = rarityMult * (1 + (level - 1) * 0.15) * dungeonStatMult;
  
  let name = '';
  const stats = {};

  if (type === ITEM_TYPES.WEAPON) {
    const prefix = WEAPON_PREFIXES[Math.floor(Math.random() * WEAPON_PREFIXES.length)];
    const bases = WEAPON_BASES[classType] || WEAPON_BASES['knight'];
    const base = bases[Math.floor(Math.random() * bases.length)];
    name = `${prefix} ${base}`;

    if (['mage', 'priest', 'bomber', 'spirit'].includes(classType)) {
      stats.magicAttack = Math.round(14 * mult);
      stats.maxMp = Math.round(14 * mult);
    } else if (classType === 'rogue') {
      stats.physicalAttack = Math.round(8 * mult);
      stats.statusPower = Math.round((0.06 + rarityMult * 0.025) * dungeonStatMult * 100) / 100;
    } else {
      stats.physicalAttack = Math.round(8 * mult);
    }
  } else if (type === ITEM_TYPES.ARMOR) {
    const prefix = ARMOR_PREFIXES[Math.floor(Math.random() * ARMOR_PREFIXES.length)];
    const bases = ARMOR_BASES[classType] || ARMOR_BASES['knight'];
    const base = bases[Math.floor(Math.random() * bases.length)];
    name = `${prefix} ${base}`;

    stats.defense = Math.round(4 * mult);
    stats.maxHp = Math.round(25 * mult);
  } else {
    const prefix = ACCESSORY_PREFIXES[Math.floor(Math.random() * ACCESSORY_PREFIXES.length)];
    const accessoryBase = pickAccessoryBase(classType);
    name = `${prefix} ${accessoryBase.name}`;

    accessoryBase.stats.forEach(statName => addAccessoryStat(stats, statName, mult, rarityMult, dungeonStatMult));

    for (let i = 0; i < getRarityBonusOptionCount(rarity); i++) {
      const bonusPool = ['maxMp', 'speed', ...RATE_STATS].filter(statName => !Object.prototype.hasOwnProperty.call(stats, statName));
      if (!bonusPool.length) break;
      const bonusStat = pickAccessoryBonusStat(bonusPool, classType);
      addAccessoryStat(stats, bonusStat, mult, rarityMult, dungeonStatMult);
    }
  }

  const upgradeLevel = 0;
  const id = `gear_${Date.now()}_${Math.floor(Math.random()*1000)}`;

  return {
    id,
    name,
    type,
    rarity,
    stats,
    upgradeLevel,
    classLimit: classType,
    description: getGearStatsDescription(stats)
  };
}

function generateNamedBossGear(classType, level, dungeonNumber) {
  const template = NAMED_BOSS_DUNGEONS[dungeonNumber];
  if (!template) return null;

  const dungeonStatMult = getDungeonGearStatMultiplier(dungeonNumber);
  const levelMult = 1 + (level - 1) * 0.15;
  const stats = {};
  Object.entries(template.stats).forEach(([statName, value]) => {
    if (isRateGearStat(statName)) {
      stats[statName] = clampGearStatValue(statName, Math.round(value * dungeonStatMult * 100) / 100);
    } else {
      stats[statName] = Math.round(value * levelMult * dungeonStatMult);
    }
  });

  return {
    id: `named_${dungeonNumber}_${Date.now()}_${Math.floor(Math.random() * 1000)}`,
    name: template.name,
    type: template.type,
    rarity: RARITIES.NAMED,
    stats,
    upgradeLevel: 0,
    classLimit: classType,
    bossOnly: true,
    dungeonNumber,
    description: getGearStatsDescription(stats)
  };
}

function getGearStatsDescription(stats) {
  return Object.entries(stats)
    .filter(([, value]) => value)
    .map(([statName, value]) => `${getGearStatLabel(statName)} +${formatGearStatValue(statName, value)}`)
    .join(', ');
}

function getUpgradeCost(gear) {
  return Math.round(25 * Math.pow(1.6, gear.upgradeLevel) * getRarityCostMultiplier(gear.rarity));
}

function upgradeGear(gear) {
  gear.upgradeLevel++;
  
  const growthFactor = 0.15;
  Object.keys(gear.stats).forEach(statName => {
    if (isRateGearStat(statName)) {
      gear.stats[statName] = clampGearStatValue(statName, Math.round(gear.stats[statName] * (1 + growthFactor) * 100) / 100);
    } else {
      gear.stats[statName] = Math.round(gear.stats[statName] * (1 + growthFactor));
    }
  });
  
  gear.description = getGearStatsDescription(gear.stats);
  return gear;
}

// Bind to window
window.RARITIES = RARITIES;
window.RARITY_MULTS = RARITY_MULTS;
window.NORMAL_RARITY_TABLE = NORMAL_RARITY_TABLE;
window.BOSS_RARITY_TABLE = BOSS_RARITY_TABLE;
window.DUNGEON_RARITY_CAPS = DUNGEON_RARITY_CAPS;
window.NORMAL_DUNGEON_RARITY_POOLS = NORMAL_DUNGEON_RARITY_POOLS;
window.DUNGEON_GEAR_STAT_MULTS = DUNGEON_GEAR_STAT_MULTS;
window.CRAFTING_RARITY_UPGRADES = CRAFTING_RARITY_UPGRADES;
window.NAMED_BOSS_DUNGEONS = NAMED_BOSS_DUNGEONS;
window.ITEM_TYPES = ITEM_TYPES;
window.SHOP_ITEMS = SHOP_ITEMS;
window.generateRandomGear = generateRandomGear;
window.generateNamedBossGear = generateNamedBossGear;
window.getRandomNormalRarity = getRandomNormalRarity;
window.getRandomBossRarity = getRandomBossRarity;
window.getMaxRarityIdForDungeon = getMaxRarityIdForDungeon;
window.filterRarityTableByDungeon = filterRarityTableByDungeon;
window.getNormalRarityIdsForDungeon = getNormalRarityIdsForDungeon;
window.filterNormalRarityTableByDungeon = filterNormalRarityTableByDungeon;
window.getDungeonGearStatMultiplier = getDungeonGearStatMultiplier;
window.getRarityMultiplier = getRarityMultiplier;
window.getRarityById = getRarityById;
window.getRarityBonusOptionCount = getRarityBonusOptionCount;
window.getCraftingRecipeData = getCraftingRecipeData;
window.getGearStatsDescription = getGearStatsDescription;
window.getGearStatLabel = getGearStatLabel;
window.formatGearStatValue = formatGearStatValue;
window.isRateGearStat = isRateGearStat;
window.getGearStatCap = getGearStatCap;
window.clampGearStatValue = clampGearStatValue;
window.getPreferredAccessoryStats = getPreferredAccessoryStats;
window.getGearSellValue = getGearSellValue;
window.getUpgradeCost = getUpgradeCost;
window.upgradeGear = upgradeGear;
