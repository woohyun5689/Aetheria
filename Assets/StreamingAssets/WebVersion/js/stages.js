// Stages & Monster Registry (Global scope)

const STAGES = [
  {
    id: 'whispering_woods',
    name: '속삭이는 숲',
    recommendedLevel: 1,
    description: '빛줄기조차 희미한 고요한 숲입니다. 야생 동물들과 고블린 무리가 어슬렁거리며 여행자를 습격합니다.',
    bossCleared: false,
    floors: 3,
    monsters: [
      {
        name: '초록 슬라임',
        icon: '🟢',
        maxHp: 45,
        maxMp: 0,
        physicalAttack: 8,
        magicAttack: 0,
        defense: 2,
        speed: 5,
        critRate: 0.02,
        xpReward: 25,
        goldReward: 10,
        skills: []
      },
      {
        name: '고블린 순찰병',
        icon: '👺',
        maxHp: 60,
        maxMp: 20,
        physicalAttack: 11,
        magicAttack: 0,
        defense: 4,
        speed: 9,
        critRate: 0.05,
        xpReward: 35,
        goldReward: 15,
        skills: [
          { name: '독침 투척', cost: 10, type: 'attack_physical_poison', power: 0.8, poisonChance: 0.5 }
        ]
      },
      {
        name: '사나운 숲늑대',
        icon: '🐺',
        maxHp: 55,
        maxMp: 0,
        physicalAttack: 13,
        magicAttack: 0,
        defense: 3,
        speed: 12,
        critRate: 0.10,
        xpReward: 40,
        goldReward: 12,
        skills: [
          { name: '물어뜯기', cost: 0, type: 'attack_physical', power: 1.2 }
        ]
      }
    ],
    boss: {
      name: '대왕 고블린 족장',
      icon: '👑',
      maxHp: 150,
      maxMp: 40,
      physicalAttack: 18,
      magicAttack: 0,
      defense: 8,
      speed: 10,
      critRate: 0.08,
      xpReward: 150,
      goldReward: 60,
      skills: [
        { name: '족장의 분노', cost: 20, type: 'attack_physical', power: 1.4 },
        { name: '대지 격타', cost: 15, type: 'attack_physical_stun', power: 1.1, stunChance: 0.17 }
      ]
    }
  },
  {
    id: 'forgotten_crypt',
    name: '잊혀진 지하묘지',
    recommendedLevel: 4,
    description: '오래전 봉인된 차가운 석실입니다. 원혼에 사로잡힌 해골 해골들과 언데드 생명체가 침입자를 기다립니다.',
    bossCleared: false,
    floors: 3,
    monsters: [
      {
        name: '부패한 좀비',
        icon: '🧟',
        maxHp: 110,
        maxMp: 0,
        physicalAttack: 16,
        magicAttack: 0,
        defense: 5,
        speed: 4,
        critRate: 0.02,
        xpReward: 65,
        goldReward: 25,
        skills: []
      },
      {
        name: '해골 검사',
        icon: '💀',
        maxHp: 130,
        maxMp: 30,
        physicalAttack: 22,
        magicAttack: 0,
        defense: 12,
        speed: 11,
        critRate: 0.06,
        xpReward: 80,
        goldReward: 30,
        skills: [
          { name: '연속 베기', cost: 15, type: 'attack_physical', power: 1.3 }
        ]
      },
      {
        name: '무덤의 유령',
        icon: '👻',
        maxHp: 90,
        maxMp: 50,
        physicalAttack: 8,
        magicAttack: 26,
        defense: 4,
        speed: 14,
        critRate: 0.12,
        xpReward: 90,
        goldReward: 35,
        skills: [
          { name: '공포의 손짓', cost: 20, type: 'attack_magic', power: 1.4, stunChance: 0.11 }
        ]
      }
    ],
    boss: {
      name: '어둠의 리치 공작',
      icon: '🧙‍♂️',
      maxHp: 320,
      maxMp: 100,
      physicalAttack: 12,
      magicAttack: 38,
      defense: 15,
      speed: 12,
      critRate: 0.10,
      xpReward: 300,
      goldReward: 150,
      skills: [
        { name: '죽음의 고리', cost: 30, type: 'attack_magic', power: 1.6, poisonChance: 0.5 },
        { name: '암흑 방벽', cost: 25, type: 'shield', power: 0.3 }
      ]
    }
  },
  {
    id: 'void_citadel',
    name: '공허의 성채',
    recommendedLevel: 7,
    description: '시공간이 일그러진 공허의 중심부에 위치한 탑입니다. 이계의 지배자와 마주하게 되는 마지막 격전지입니다.',
    bossCleared: false,
    floors: 4,
    monsters: [
      {
        name: '공허 사냥개',
        icon: '👾',
        maxHp: 210,
        maxMp: 40,
        physicalAttack: 32,
        magicAttack: 12,
        defense: 14,
        speed: 20,
        critRate: 0.15,
        xpReward: 160,
        goldReward: 50,
        skills: [
          { name: '위동 물어뜯기', cost: 15, type: 'attack_physical_poison', power: 1.3, poisonChance: 0.5 }
        ]
      },
      {
        name: '심연의 차원 감시자',
        icon: '👁️',
        maxHp: 190,
        maxMp: 120,
        physicalAttack: 15,
        magicAttack: 45,
        defense: 10,
        speed: 18,
        critRate: 0.12,
        xpReward: 180,
        goldReward: 60,
        skills: [
          { name: '붕괴 레이저', cost: 25, type: 'attack_magic', power: 1.5, burnChance: 0.4 }
        ]
      },
      {
        name: '공허의 집행기사',
        icon: '⚔️',
        maxHp: 270,
        maxMp: 50,
        physicalAttack: 38,
        magicAttack: 0,
        defense: 25,
        speed: 14,
        critRate: 0.08,
        xpReward: 210,
        goldReward: 65,
        skills: [
          { name: '파쇄 참격', cost: 20, type: 'attack_physical', power: 1.4 }
        ]
      }
    ],
    boss: {
      name: '공허의 파괴 군주 바알',
      icon: '😈',
      maxHp: 650,
      maxMp: 200,
      physicalAttack: 48,
      magicAttack: 55,
      defense: 30,
      speed: 17,
      critRate: 0.15,
      xpReward: 1000,
      goldReward: 500,
      skills: [
        { name: '멸망의 전조', cost: 40, type: 'attack_magic', power: 1.8, burnChance: 0.8 },
        { name: '공허의 파동', cost: 30, type: 'attack_physical_stun', power: 1.4, stunChance: 0.22 },
        { name: '절대 보호막', cost: 35, type: 'shield', power: 0.4 }
      ]
    }
  },
  {
    id: 'crimson_mine',
    name: '붉은 수정 광산',
    recommendedLevel: 10,
    description: '붉게 빛나는 수정맥이 지하 깊은 곳에서 맥동합니다. 광산을 점거한 괴물들이 수정의 힘에 취해 난폭해졌습니다.',
    bossCleared: false,
    floors: 4,
    monsters: [
      {
        name: '수정 광부 좀비',
        icon: '⛏️',
        maxHp: 340,
        maxMp: 40,
        physicalAttack: 52,
        magicAttack: 8,
        defense: 34,
        speed: 14,
        critRate: 0.08,
        xpReward: 280,
        goldReward: 85,
        skills: [
          { name: '곡괭이 강타', cost: 15, type: 'attack_physical_stun', power: 1.35, stunChance: 0.11 }
        ]
      },
      {
        name: '혈정 박쥐',
        icon: '🦇',
        maxHp: 260,
        maxMp: 80,
        physicalAttack: 48,
        magicAttack: 22,
        defense: 18,
        speed: 28,
        critRate: 0.18,
        xpReward: 300,
        goldReward: 90,
        skills: [
          { name: '흡혈 급습', cost: 20, type: 'attack_physical_crit', power: 1.45 }
        ]
      },
      {
        name: '수정 골렘',
        icon: '💎',
        maxHp: 430,
        maxMp: 50,
        physicalAttack: 55,
        magicAttack: 18,
        defense: 46,
        speed: 9,
        critRate: 0.05,
        xpReward: 340,
        goldReward: 110,
        skills: [
          { name: '수정 파편', cost: 20, type: 'attack_magic', power: 1.35, burnChance: 0.25 }
        ]
      }
    ],
    boss: {
      name: '혈정 거인 그라논',
      icon: '🗿',
      maxHp: 980,
      maxMp: 160,
      physicalAttack: 72,
      magicAttack: 32,
      defense: 55,
      speed: 12,
      critRate: 0.10,
      xpReward: 1600,
      goldReward: 700,
      skills: [
        { name: '붉은 붕괴', cost: 35, type: 'attack_physical_stun', power: 1.65, stunChance: 0.19 },
        { name: '수정 갑피', cost: 30, type: 'shield', power: 0.35 }
      ]
    }
  },
  {
    id: 'storm_spire',
    name: '폭풍 첨탑',
    recommendedLevel: 13,
    description: '벼락이 멈추지 않는 하늘의 탑입니다. 바람 정령과 번개 괴수들이 침입자의 균형을 무너뜨립니다.',
    bossCleared: false,
    floors: 4,
    monsters: [
      {
        name: '돌풍 정령',
        icon: '🌪️',
        maxHp: 390,
        maxMp: 120,
        physicalAttack: 38,
        magicAttack: 66,
        defense: 26,
        speed: 34,
        critRate: 0.14,
        xpReward: 430,
        goldReward: 125,
        skills: [
          { name: '칼바람', cost: 25, type: 'attack_magic_crit', power: 1.4 }
        ]
      },
      {
        name: '번개 와이번',
        icon: '🐉',
        maxHp: 470,
        maxMp: 130,
        physicalAttack: 64,
        magicAttack: 58,
        defense: 32,
        speed: 30,
        critRate: 0.16,
        xpReward: 470,
        goldReward: 145,
        skills: [
          { name: '뇌격 숨결', cost: 30, type: 'attack_magic_stun', power: 1.45, stunChance: 0.14 }
        ]
      },
      {
        name: '먹구름 사제',
        icon: '⛈️',
        maxHp: 410,
        maxMp: 180,
        physicalAttack: 22,
        magicAttack: 78,
        defense: 28,
        speed: 20,
        critRate: 0.10,
        xpReward: 490,
        goldReward: 155,
        skills: [
          { name: '폭풍 의식', cost: 35, type: 'attack_magic', power: 1.55, burnChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '천둥 군주 라이에스',
      icon: '⚡',
      maxHp: 1250,
      maxMp: 260,
      physicalAttack: 78,
      magicAttack: 94,
      defense: 42,
      speed: 28,
      critRate: 0.16,
      xpReward: 2300,
      goldReward: 950,
      skills: [
        { name: '천벌 낙뢰', cost: 45, type: 'attack_magic_stun', power: 1.75, stunChance: 0.22 },
        { name: '폭풍 장막', cost: 35, type: 'shield', power: 0.38 }
      ]
    }
  },
  {
    id: 'frozen_palace',
    name: '얼어붙은 왕궁',
    recommendedLevel: 16,
    description: '시간마저 얼어붙은 폐궁입니다. 차가운 기사단과 얼음 마법이 발걸음을 늦춥니다.',
    bossCleared: false,
    floors: 4,
    monsters: [
      {
        name: '서리 창병',
        icon: '🧊',
        maxHp: 560,
        maxMp: 90,
        physicalAttack: 82,
        magicAttack: 34,
        defense: 54,
        speed: 18,
        critRate: 0.09,
        xpReward: 620,
        goldReward: 180,
        skills: [
          { name: '빙결 찌르기', cost: 25, type: 'attack_physical_stun', power: 1.45, stunChance: 0.14 }
        ]
      },
      {
        name: '눈보라 마녀',
        icon: '🧙',
        maxHp: 480,
        maxMp: 220,
        physicalAttack: 28,
        magicAttack: 98,
        defense: 36,
        speed: 24,
        critRate: 0.12,
        xpReward: 660,
        goldReward: 205,
        skills: [
          { name: '서리 파도', cost: 40, type: 'attack_magic', power: 1.65 }
        ]
      },
      {
        name: '빙벽 수호자',
        icon: '🛡️',
        maxHp: 680,
        maxMp: 80,
        physicalAttack: 76,
        magicAttack: 20,
        defense: 70,
        speed: 12,
        critRate: 0.06,
        xpReward: 700,
        goldReward: 220,
        skills: [
          { name: '얼음 방패', cost: 25, type: 'shield', power: 0.25 }
        ]
      }
    ],
    boss: {
      name: '빙관의 여왕 세레나',
      icon: '👸',
      maxHp: 1680,
      maxMp: 360,
      physicalAttack: 70,
      magicAttack: 128,
      defense: 64,
      speed: 22,
      critRate: 0.14,
      xpReward: 3200,
      goldReward: 1250,
      skills: [
        { name: '영원의 동결', cost: 55, type: 'attack_magic_stun', power: 1.8, stunChance: 0.25 },
        { name: '서리 왕관', cost: 45, type: 'heal_shield', power: 0.5 }
      ]
    }
  },
  {
    id: 'ashen_wasteland',
    name: '잿빛 황무지',
    recommendedLevel: 20,
    description: '불타고 남은 대지 위로 독기와 재가 떠돕니다. 생존한 괴수들은 상처 입은 세계처럼 거칠고 집요합니다.',
    bossCleared: false,
    floors: 5,
    monsters: [
      {
        name: '잿불 약탈자',
        icon: '🔥',
        maxHp: 760,
        maxMp: 120,
        physicalAttack: 104,
        magicAttack: 46,
        defense: 66,
        speed: 24,
        critRate: 0.14,
        xpReward: 900,
        goldReward: 260,
        skills: [
          { name: '불타는 도끼', cost: 30, type: 'attack_physical', power: 1.55, burnChance: 0.35 }
        ]
      },
      {
        name: '독안개 괴수',
        icon: '☠️',
        maxHp: 840,
        maxMp: 150,
        physicalAttack: 92,
        magicAttack: 60,
        defense: 58,
        speed: 18,
        critRate: 0.08,
        xpReward: 940,
        goldReward: 275,
        skills: [
          { name: '맹독 구름', cost: 35, type: 'attack_magic_poison', power: 1.45, poisonChance: 0.7 }
        ]
      },
      {
        name: '검은 재의 기사',
        icon: '♞',
        maxHp: 900,
        maxMp: 100,
        physicalAttack: 116,
        magicAttack: 24,
        defense: 78,
        speed: 20,
        critRate: 0.11,
        xpReward: 980,
        goldReward: 290,
        skills: [
          { name: '회색 참수', cost: 35, type: 'attack_physical_crit', power: 1.6 }
        ]
      }
    ],
    boss: {
      name: '화산 심장 모르칸',
      icon: '🌋',
      maxHp: 2400,
      maxMp: 420,
      physicalAttack: 138,
      magicAttack: 116,
      defense: 86,
      speed: 20,
      critRate: 0.15,
      xpReward: 4600,
      goldReward: 1700,
      skills: [
        { name: '용암 폭주', cost: 60, type: 'attack_magic', power: 1.9, burnChance: 0.75 },
        { name: '지각 분쇄', cost: 50, type: 'attack_physical_stun', power: 1.75, stunChance: 0.19 }
      ]
    }
  },
  {
    id: 'moonlit_labyrinth',
    name: '월광 미궁',
    recommendedLevel: 24,
    description: '달빛을 따라 길이 바뀌는 미궁입니다. 환영과 암살자들이 방향감각을 흔듭니다.',
    bossCleared: false,
    floors: 5,
    monsters: [
      {
        name: '달그림자 추적자',
        icon: '🌙',
        maxHp: 980,
        maxMp: 160,
        physicalAttack: 138,
        magicAttack: 72,
        defense: 72,
        speed: 38,
        critRate: 0.22,
        xpReward: 1250,
        goldReward: 350,
        skills: [
          { name: '월광 급습', cost: 40, type: 'attack_physical_crit', power: 1.7 }
        ]
      },
      {
        name: '거울 환영술사',
        icon: '🪞',
        maxHp: 900,
        maxMp: 260,
        physicalAttack: 44,
        magicAttack: 150,
        defense: 60,
        speed: 30,
        critRate: 0.16,
        xpReward: 1300,
        goldReward: 375,
        skills: [
          { name: '환영 붕괴', cost: 50, type: 'attack_magic_stun', power: 1.65, stunChance: 0.17 }
        ]
      },
      {
        name: '미궁 살수',
        icon: '🗡️',
        maxHp: 940,
        maxMp: 130,
        physicalAttack: 154,
        magicAttack: 50,
        defense: 64,
        speed: 42,
        critRate: 0.24,
        xpReward: 1340,
        goldReward: 390,
        skills: [
          { name: '독월 베기', cost: 45, type: 'attack_physical_poison', power: 1.6, poisonChance: 0.65 }
        ]
      }
    ],
    boss: {
      name: '미궁의 은월 아리아',
      icon: '🌕',
      maxHp: 3100,
      maxMp: 520,
      physicalAttack: 158,
      magicAttack: 170,
      defense: 82,
      speed: 40,
      critRate: 0.22,
      xpReward: 6200,
      goldReward: 2200,
      skills: [
        { name: '은월 심판', cost: 70, type: 'attack_magic_crit', power: 1.9 },
        { name: '그림자 결박', cost: 55, type: 'attack_physical_stun', power: 1.65, stunChance: 0.22 }
      ]
    }
  },
  {
    id: 'sunken_archive',
    name: '가라앉은 기록보관소',
    recommendedLevel: 28,
    description: '고대 지식이 바닷물 아래 잠든 장소입니다. 잊힌 문서와 수중 괴물들이 금지된 주문을 지킵니다.',
    bossCleared: false,
    floors: 5,
    monsters: [
      {
        name: '심해 필경사',
        icon: '📜',
        maxHp: 1180,
        maxMp: 340,
        physicalAttack: 62,
        magicAttack: 188,
        defense: 82,
        speed: 24,
        critRate: 0.15,
        xpReward: 1650,
        goldReward: 470,
        skills: [
          { name: '금서 낭독', cost: 60, type: 'attack_magic_poison', power: 1.7, poisonChance: 0.5 }
        ]
      },
      {
        name: '해구 파수꾼',
        icon: '🦑',
        maxHp: 1380,
        maxMp: 200,
        physicalAttack: 172,
        magicAttack: 82,
        defense: 100,
        speed: 22,
        critRate: 0.12,
        xpReward: 1720,
        goldReward: 500,
        skills: [
          { name: '촉수 압박', cost: 55, type: 'attack_physical_stun', power: 1.75, stunChance: 0.17 }
        ]
      },
      {
        name: '침수된 골렘',
        icon: '🧱',
        maxHp: 1580,
        maxMp: 120,
        physicalAttack: 166,
        magicAttack: 44,
        defense: 126,
        speed: 12,
        critRate: 0.07,
        xpReward: 1780,
        goldReward: 520,
        skills: [
          { name: '수압 강타', cost: 50, type: 'attack_physical', power: 1.8 }
        ]
      }
    ],
    boss: {
      name: '심해 기록관 노틸루스',
      icon: '🐙',
      maxHp: 4200,
      maxMp: 760,
      physicalAttack: 178,
      magicAttack: 220,
      defense: 124,
      speed: 22,
      critRate: 0.16,
      xpReward: 8200,
      goldReward: 2850,
      skills: [
        { name: '금단의 조류', cost: 80, type: 'attack_magic_poison', power: 1.95, poisonChance: 0.65 },
        { name: '해저 봉인', cost: 70, type: 'attack_magic_stun', power: 1.65, stunChance: 0.25 }
      ]
    }
  },
  {
    id: 'eternal_rift',
    name: '영원의 균열',
    recommendedLevel: 38,
    description: '모든 차원의 끝과 시작이 겹쳐진 마지막 균열입니다. 현실의 법칙을 벗어난 존재들이 영웅의 한계를 시험합니다.',
    bossCleared: false,
    floors: 6,
    monsters: [
      {
        name: '시간 파편수',
        icon: '⏳',
        maxHp: 2100,
        maxMp: 520,
        physicalAttack: 260,
        magicAttack: 210,
        defense: 160,
        speed: 38,
        critRate: 0.18,
        xpReward: 2900,
        goldReward: 820,
        skills: [
          { name: '시간 절단', cost: 90, type: 'attack_physical_stun', power: 1.9, stunChance: 0.19 }
        ]
      },
      {
        name: '차원 심판관',
        icon: '⚖️',
        maxHp: 2300,
        maxMp: 620,
        physicalAttack: 220,
        magicAttack: 280,
        defense: 170,
        speed: 28,
        critRate: 0.16,
        xpReward: 3050,
        goldReward: 860,
        skills: [
          { name: '차원 판결', cost: 95, type: 'attack_magic_stun', power: 1.95, stunChance: 0.17 }
        ]
      },
      {
        name: '무한의 포식자',
        icon: '🌀',
        maxHp: 2600,
        maxMp: 480,
        physicalAttack: 300,
        magicAttack: 180,
        defense: 150,
        speed: 34,
        critRate: 0.22,
        xpReward: 3200,
        goldReward: 900,
        skills: [
          { name: '공허 포식', cost: 85, type: 'attack_physical_poison', power: 2.0, poisonChance: 0.55 }
        ]
      }
    ],
    boss: {
      name: '영겁의 핵 아스트라온',
      icon: '🌌',
      maxHp: 7800,
      maxMp: 1300,
      physicalAttack: 320,
      magicAttack: 360,
      defense: 190,
      speed: 34,
      critRate: 0.24,
      xpReward: 15000,
      goldReward: 5000,
      skills: [
        { name: '영겁 붕괴', cost: 120, type: 'attack_magic_crit', power: 2.2 },
        { name: '무한 낙인', cost: 105, type: 'attack_physical_poison', power: 2.0, poisonChance: 0.7 },
        { name: '차원 재구성', cost: 100, type: 'heal_shield', power: 0.55 }
      ]
    }
  },
  {
    id: 'obsidian_sanctum',
    name: '흑요석 성소',
    recommendedLevel: 44,
    description: '검은 성석이 끝없이 자라나는 성소입니다. 공허를 숭배하는 수호자들이 침입자를 제물로 삼으려 합니다.',
    bossCleared: false,
    floors: 6,
    monsters: [
      {
        name: '흑요석 파수병',
        icon: '🗿',
        maxHp: 3000,
        maxMp: 520,
        physicalAttack: 350,
        magicAttack: 170,
        defense: 220,
        speed: 32,
        critRate: 0.16,
        xpReward: 3900,
        goldReward: 1050,
        skills: [
          { name: '검은 파쇄', cost: 95, type: 'attack_physical_stun', power: 1.95, stunChance: 0.14 }
        ]
      },
      {
        name: '성소 주술사',
        icon: '🕯️',
        maxHp: 2700,
        maxMp: 900,
        physicalAttack: 160,
        magicAttack: 390,
        defense: 165,
        speed: 36,
        critRate: 0.18,
        xpReward: 4100,
        goldReward: 1120,
        skills: [
          { name: '저주 성화', cost: 110, type: 'attack_magic_poison', power: 2.0, poisonChance: 0.58 }
        ]
      }
    ],
    boss: {
      name: '흑요 대사제 카르복스',
      icon: '🕋',
      maxHp: 9800,
      maxMp: 1700,
      physicalAttack: 390,
      magicAttack: 430,
      defense: 245,
      speed: 34,
      critRate: 0.20,
      xpReward: 19000,
      goldReward: 6100,
      skills: [
        { name: '성소 붕괴', cost: 135, type: 'attack_magic', power: 2.25, burnChance: 0.6 },
        { name: '흑요 감옥', cost: 115, type: 'attack_physical_stun', power: 2.0, stunChance: 0.17 }
      ]
    }
  },
  {
    id: 'starfall_bastion',
    name: '별추락 요새',
    recommendedLevel: 50,
    description: '추락한 별의 파편으로 지어진 요새입니다. 별빛에 뒤틀린 전사들이 강렬한 일격을 퍼붓습니다.',
    bossCleared: false,
    floors: 6,
    monsters: [
      {
        name: '성흔 기사',
        icon: '🌠',
        maxHp: 3600,
        maxMp: 640,
        physicalAttack: 420,
        magicAttack: 260,
        defense: 240,
        speed: 38,
        critRate: 0.22,
        xpReward: 4700,
        goldReward: 1260,
        skills: [
          { name: '유성 절단', cost: 115, type: 'attack_physical_crit', power: 2.1 }
        ]
      },
      {
        name: '별빛 포격수',
        icon: '☄️',
        maxHp: 3200,
        maxMp: 980,
        physicalAttack: 190,
        magicAttack: 470,
        defense: 190,
        speed: 35,
        critRate: 0.17,
        xpReward: 4900,
        goldReward: 1320,
        skills: [
          { name: '낙성 포화', cost: 125, type: 'attack_magic', power: 2.15, burnChance: 0.62 }
        ]
      }
    ],
    boss: {
      name: '추락성 아르카엘',
      icon: '🌟',
      maxHp: 12200,
      maxMp: 2100,
      physicalAttack: 470,
      magicAttack: 520,
      defense: 275,
      speed: 40,
      critRate: 0.24,
      xpReward: 24000,
      goldReward: 7200,
      skills: [
        { name: '별의 심판', cost: 155, type: 'attack_magic_crit', power: 2.35 },
        { name: '중력 붕괴', cost: 130, type: 'attack_magic_stun', power: 2.05, stunChance: 0.18 }
      ]
    }
  },
  {
    id: 'dream_mire',
    name: '몽환 늪지',
    recommendedLevel: 56,
    description: '꿈과 독기가 뒤섞인 늪입니다. 몬스터의 형체가 흐릿하게 흔들리며 장기전을 강요합니다.',
    bossCleared: false,
    floors: 6,
    monsters: [
      {
        name: '몽독 수렁괴물',
        icon: '🫧',
        maxHp: 4200,
        maxMp: 720,
        physicalAttack: 430,
        magicAttack: 310,
        defense: 285,
        speed: 28,
        critRate: 0.14,
        xpReward: 5700,
        goldReward: 1450,
        skills: [
          { name: '환각 독무', cost: 125, type: 'attack_magic_poison', power: 1.95, poisonChance: 0.68 }
        ]
      },
      {
        name: '잠식된 몽령',
        icon: '💤',
        maxHp: 3600,
        maxMp: 1180,
        physicalAttack: 160,
        magicAttack: 560,
        defense: 210,
        speed: 44,
        critRate: 0.20,
        xpReward: 5900,
        goldReward: 1520,
        skills: [
          { name: '꿈결 속박', cost: 135, type: 'attack_magic_stun', power: 1.95, stunChance: 0.16 }
        ]
      }
    ],
    boss: {
      name: '잠든 여왕 모르페나',
      icon: '🌙',
      maxHp: 14800,
      maxMp: 2600,
      physicalAttack: 390,
      magicAttack: 620,
      defense: 310,
      speed: 42,
      critRate: 0.22,
      xpReward: 30000,
      goldReward: 8300,
      skills: [
        { name: '영면의 독안개', cost: 170, type: 'attack_magic_poison', power: 2.25, poisonChance: 0.72 },
        { name: '몽환 장막', cost: 140, type: 'heal_shield', power: 0.48 }
      ]
    }
  },
  {
    id: 'ironwood_grove',
    name: '강철나무 거목림',
    recommendedLevel: 62,
    description: '나무껍질이 금속처럼 단단한 숲입니다. 느리지만 무거운 공격과 높은 방어력이 특징입니다.',
    bossCleared: false,
    floors: 7,
    monsters: [
      {
        name: '강철가지 파괴자',
        icon: '🌲',
        maxHp: 5200,
        maxMp: 560,
        physicalAttack: 560,
        magicAttack: 120,
        defense: 360,
        speed: 24,
        critRate: 0.12,
        xpReward: 6800,
        goldReward: 1680,
        skills: [
          { name: '철목 강타', cost: 125, type: 'attack_physical_stun', power: 2.15, stunChance: 0.15 }
        ]
      },
      {
        name: '녹슨 수액 정령',
        icon: '🍂',
        maxHp: 4700,
        maxMp: 900,
        physicalAttack: 300,
        magicAttack: 500,
        defense: 320,
        speed: 30,
        critRate: 0.13,
        xpReward: 7050,
        goldReward: 1740,
        skills: [
          { name: '부식 수액', cost: 135, type: 'attack_magic_poison', power: 2.05, poisonChance: 0.62 }
        ]
      }
    ],
    boss: {
      name: '강철뿌리 오르다곤',
      icon: '🌳',
      maxHp: 19000,
      maxMp: 2100,
      physicalAttack: 650,
      magicAttack: 430,
      defense: 430,
      speed: 26,
      critRate: 0.16,
      xpReward: 37000,
      goldReward: 9600,
      skills: [
        { name: '대지 고정', cost: 155, type: 'attack_physical_stun', power: 2.25, stunChance: 0.18 },
        { name: '철목 재생', cost: 150, type: 'heal_shield', power: 0.42 }
      ]
    }
  },
  {
    id: 'aurora_prison',
    name: '오로라 감옥',
    recommendedLevel: 68,
    description: '빛의 결계가 수감자를 가두는 감옥입니다. 눈부신 마법과 빙결의 압박이 이어집니다.',
    bossCleared: false,
    floors: 7,
    monsters: [
      {
        name: '오로라 간수',
        icon: '🔐',
        maxHp: 5600,
        maxMp: 1050,
        physicalAttack: 360,
        magicAttack: 620,
        defense: 340,
        speed: 38,
        critRate: 0.18,
        xpReward: 7900,
        goldReward: 1900,
        skills: [
          { name: '광휘 족쇄', cost: 145, type: 'attack_magic_stun', power: 2.1, stunChance: 0.17 }
        ]
      },
      {
        name: '빙광 죄수',
        icon: '🧊',
        maxHp: 6200,
        maxMp: 760,
        physicalAttack: 610,
        magicAttack: 300,
        defense: 380,
        speed: 32,
        critRate: 0.15,
        xpReward: 8150,
        goldReward: 1980,
        skills: [
          { name: '얼어붙은 난동', cost: 135, type: 'attack_physical', power: 2.25 }
        ]
      }
    ],
    boss: {
      name: '빛감옥장 루미라',
      icon: '🌈',
      maxHp: 22500,
      maxMp: 3100,
      physicalAttack: 520,
      magicAttack: 760,
      defense: 430,
      speed: 38,
      critRate: 0.20,
      xpReward: 45000,
      goldReward: 10800,
      skills: [
        { name: '극광 처형', cost: 190, type: 'attack_magic_crit', power: 2.45 },
        { name: '봉인 광선', cost: 160, type: 'attack_magic_stun', power: 2.05, stunChance: 0.18 }
      ]
    }
  },
  {
    id: 'abyssal_observatory',
    name: '심연 관측소',
    recommendedLevel: 74,
    description: '공허 너머를 관측하던 탑입니다. 별 사이의 괴물들이 계산된 마법으로 압박합니다.',
    bossCleared: false,
    floors: 7,
    monsters: [
      {
        name: '심연 점성술사',
        icon: '🔭',
        maxHp: 6500,
        maxMp: 1500,
        physicalAttack: 240,
        magicAttack: 780,
        defense: 370,
        speed: 42,
        critRate: 0.20,
        xpReward: 9200,
        goldReward: 2200,
        skills: [
          { name: '별자리 붕괴', cost: 165, type: 'attack_magic', power: 2.35, burnChance: 0.55 }
        ]
      },
      {
        name: '망원경 포식체',
        icon: '👁️',
        maxHp: 7200,
        maxMp: 980,
        physicalAttack: 720,
        magicAttack: 420,
        defense: 410,
        speed: 36,
        critRate: 0.22,
        xpReward: 9500,
        goldReward: 2280,
        skills: [
          { name: '시야 절단', cost: 155, type: 'attack_physical_crit', power: 2.35 }
        ]
      }
    ],
    boss: {
      name: '무저성 관측자 엘드라',
      icon: '🪐',
      maxHp: 26000,
      maxMp: 3800,
      physicalAttack: 620,
      magicAttack: 880,
      defense: 470,
      speed: 44,
      critRate: 0.22,
      xpReward: 54000,
      goldReward: 12400,
      skills: [
        { name: '무저점 관측', cost: 210, type: 'attack_magic_stun', power: 2.35, stunChance: 0.17 },
        { name: '공허 좌표', cost: 190, type: 'attack_magic_poison', power: 2.25, poisonChance: 0.65 }
      ]
    }
  },
  {
    id: 'bloodmoon_cathedral',
    name: '핏달 대성당',
    recommendedLevel: 80,
    description: '붉은 달빛이 스테인드글라스를 타고 쏟아지는 대성당입니다. 흡혈과 치명타 공격이 위협적입니다.',
    bossCleared: false,
    floors: 7,
    monsters: [
      {
        name: '핏달 사제',
        icon: '🩸',
        maxHp: 7600,
        maxMp: 1320,
        physicalAttack: 420,
        magicAttack: 760,
        defense: 420,
        speed: 40,
        critRate: 0.24,
        xpReward: 10600,
        goldReward: 2500,
        skills: [
          { name: '혈월 저주', cost: 170, type: 'attack_magic_poison', power: 2.2, poisonChance: 0.7 }
        ]
      },
      {
        name: '적월 성기사',
        icon: '🛡️',
        maxHp: 8400,
        maxMp: 860,
        physicalAttack: 820,
        magicAttack: 260,
        defense: 500,
        speed: 34,
        critRate: 0.20,
        xpReward: 10900,
        goldReward: 2580,
        skills: [
          { name: '피의 참회', cost: 160, type: 'attack_physical_stun', power: 2.35, stunChance: 0.16 }
        ]
      }
    ],
    boss: {
      name: '붉은 대주교 베르나크',
      icon: '🦇',
      maxHp: 30500,
      maxMp: 4200,
      physicalAttack: 780,
      magicAttack: 940,
      defense: 540,
      speed: 40,
      critRate: 0.26,
      xpReward: 65000,
      goldReward: 14500,
      skills: [
        { name: '핏달 강림', cost: 230, type: 'attack_magic_crit', power: 2.55 },
        { name: '성혈 속박', cost: 190, type: 'attack_magic_stun', power: 2.2, stunChance: 0.18 }
      ]
    }
  },
  {
    id: 'crystal_timeway',
    name: '수정 시간로',
    recommendedLevel: 86,
    description: '수정 속에 시간이 접힌 길입니다. 빠른 적들이 턴을 흔들고 강한 지속 피해를 남깁니다.',
    bossCleared: false,
    floors: 8,
    monsters: [
      {
        name: '시간 유리검사',
        icon: '⏳',
        maxHp: 8800,
        maxMp: 1100,
        physicalAttack: 920,
        magicAttack: 360,
        defense: 520,
        speed: 54,
        critRate: 0.25,
        xpReward: 12300,
        goldReward: 2850,
        skills: [
          { name: '초침 난무', cost: 175, type: 'attack_physical_crit', power: 2.45 }
        ]
      },
      {
        name: '수정 시계마녀',
        icon: '⌛',
        maxHp: 8000,
        maxMp: 1850,
        physicalAttack: 300,
        magicAttack: 980,
        defense: 460,
        speed: 48,
        critRate: 0.21,
        xpReward: 12600,
        goldReward: 2940,
        skills: [
          { name: '균열 초침', cost: 190, type: 'attack_magic_poison', power: 2.3, poisonChance: 0.68 }
        ]
      }
    ],
    boss: {
      name: '시간결정 크로노스핀',
      icon: '💎',
      maxHp: 35000,
      maxMp: 5000,
      physicalAttack: 900,
      magicAttack: 1080,
      defense: 600,
      speed: 52,
      critRate: 0.26,
      xpReward: 78000,
      goldReward: 16600,
      skills: [
        { name: '영겁 회전', cost: 250, type: 'attack_magic_stun', power: 2.45, stunChance: 0.17 },
        { name: '수정 역류', cost: 220, type: 'heal_shield', power: 0.46 }
      ]
    }
  },
  {
    id: 'voidforge_nexus',
    name: '공허 대장간 핵',
    recommendedLevel: 92,
    description: '차원을 벼려 무기로 만드는 핵심 대장간입니다. 무겁고 뜨거운 공격이 방어를 시험합니다.',
    bossCleared: false,
    floors: 8,
    monsters: [
      {
        name: '공허 제련수',
        icon: '⚒️',
        maxHp: 9800,
        maxMp: 1200,
        physicalAttack: 1040,
        magicAttack: 520,
        defense: 640,
        speed: 34,
        critRate: 0.20,
        xpReward: 14000,
        goldReward: 3200,
        skills: [
          { name: '차원 망치', cost: 190, type: 'attack_physical_stun', power: 2.55, stunChance: 0.16 }
        ]
      },
      {
        name: '용광로 악령',
        icon: '🔥',
        maxHp: 9100,
        maxMp: 2100,
        physicalAttack: 480,
        magicAttack: 1120,
        defense: 560,
        speed: 38,
        critRate: 0.22,
        xpReward: 14400,
        goldReward: 3300,
        skills: [
          { name: '검은 용암', cost: 210, type: 'attack_magic', power: 2.5, burnChance: 0.75 }
        ]
      }
    ],
    boss: {
      name: '차원 대장장이 모르둠',
      icon: '🏭',
      maxHp: 41000,
      maxMp: 5600,
      physicalAttack: 1180,
      magicAttack: 980,
      defense: 700,
      speed: 36,
      critRate: 0.24,
      xpReward: 92000,
      goldReward: 18800,
      skills: [
        { name: '세계 단조', cost: 270, type: 'attack_physical_stun', power: 2.7, stunChance: 0.18 },
        { name: '공허 담금질', cost: 240, type: 'attack_magic', power: 2.45, burnChance: 0.7 }
      ]
    }
  },
  {
    id: 'origin_throne',
    name: '태초의 왕좌',
    recommendedLevel: 100,
    description: '모든 균열의 첫 숨이 남아 있는 왕좌입니다. 마지막 공허가 영웅의 모든 성장을 시험합니다.',
    bossCleared: false,
    floors: 9,
    monsters: [
      {
        name: '태초의 파편',
        icon: '✨',
        maxHp: 11200,
        maxMp: 2400,
        physicalAttack: 980,
        magicAttack: 1220,
        defense: 680,
        speed: 46,
        critRate: 0.24,
        xpReward: 16500,
        goldReward: 3600,
        skills: [
          { name: '기원 붕괴', cost: 230, type: 'attack_magic_poison', power: 2.55, poisonChance: 0.72 }
        ]
      },
      {
        name: '왕좌의 집행자',
        icon: '👑',
        maxHp: 12500,
        maxMp: 1800,
        physicalAttack: 1280,
        magicAttack: 640,
        defense: 760,
        speed: 42,
        critRate: 0.23,
        xpReward: 17000,
        goldReward: 3800,
        skills: [
          { name: '창세 참격', cost: 220, type: 'attack_physical_crit', power: 2.65 }
        ]
      }
    ],
    boss: {
      name: '태초의 공허 제네시온',
      icon: '🜂',
      maxHp: 52000,
      maxMp: 7200,
      physicalAttack: 1380,
      magicAttack: 1450,
      defense: 820,
      speed: 48,
      critRate: 0.28,
      xpReward: 120000,
      goldReward: 25000,
      skills: [
        { name: '태초 붕괴', cost: 320, type: 'attack_magic_crit', power: 2.85 },
        { name: '왕좌 단죄', cost: 290, type: 'attack_physical_stun', power: 2.65, stunChance: 0.18 },
        { name: '공허 재탄생', cost: 280, type: 'heal_shield', power: 0.5 }
      ]
    }
  },
  {
    id: 'radiant_oratory',
    name: '찬란한 성가당',
    recommendedLevel: 108,
    description: '빛이 지나치게 응축되어 눈을 멀게 하는 예배당입니다. 성가의 잔향이 적과 아군을 동시에 시험합니다.',
    bossCleared: false,
    floors: 9,
    monsters: [
      {
        name: '눈부신 순례자',
        icon: '✨',
        maxHp: 14200,
        maxMp: 3200,
        physicalAttack: 920,
        magicAttack: 1540,
        defense: 860,
        speed: 52,
        critRate: 0.25,
        xpReward: 19500,
        goldReward: 4300,
        skills: [
          { name: '섬광 찬가', cost: 260, type: 'attack_magic', power: 2.75, blindChance: 0.45 }
        ]
      },
      {
        name: '성역 파수병',
        icon: '🛡️',
        maxHp: 16800,
        maxMp: 2600,
        physicalAttack: 1480,
        magicAttack: 920,
        defense: 980,
        speed: 42,
        critRate: 0.22,
        xpReward: 20200,
        goldReward: 4500,
        skills: [
          { name: '눈부신 철퇴', cost: 240, type: 'attack_physical_stun', power: 2.75, stunChance: 0.16 }
        ]
      }
    ],
    boss: {
      name: '찬란한 심판관 루멘',
      icon: '⚖️',
      maxHp: 65000,
      maxMp: 9000,
      physicalAttack: 1520,
      magicAttack: 1780,
      defense: 980,
      speed: 50,
      critRate: 0.28,
      xpReward: 150000,
      goldReward: 30000,
      skills: [
        { name: '백광 판결', cost: 360, type: 'attack_magic_crit', power: 3.0, blindChance: 0.55 },
        { name: '심판의 종', cost: 320, type: 'attack_magic', power: 2.8, silenceChance: 0.35 }
      ]
    }
  },
  {
    id: 'thunder_hollow',
    name: '천둥 공동',
    recommendedLevel: 116,
    description: '번개가 바위 속을 흐르는 거대한 공동입니다. 감전 피해와 기습적인 기절을 조심해야 합니다.',
    bossCleared: false,
    floors: 9,
    monsters: [
      {
        name: '전류 포식자',
        icon: '⚡',
        maxHp: 17000,
        maxMp: 3600,
        physicalAttack: 1120,
        magicAttack: 1680,
        defense: 900,
        speed: 58,
        critRate: 0.27,
        xpReward: 22500,
        goldReward: 4900,
        skills: [
          { name: '전류 물어뜯기', cost: 280, type: 'attack_magic', power: 2.9, shockChance: 0.62 }
        ]
      },
      {
        name: '낙뢰 기사',
        icon: '🏇',
        maxHp: 18600,
        maxMp: 3000,
        physicalAttack: 1650,
        magicAttack: 1180,
        defense: 1040,
        speed: 55,
        critRate: 0.26,
        xpReward: 23200,
        goldReward: 5100,
        skills: [
          { name: '낙뢰 돌격', cost: 270, type: 'attack_physical_stun', power: 2.95, stunChance: 0.17, shockChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '공동의 천둥룡 브론테',
      icon: '🐉',
      maxHp: 76000,
      maxMp: 10400,
      physicalAttack: 1780,
      magicAttack: 1980,
      defense: 1120,
      speed: 57,
      critRate: 0.3,
      xpReward: 176000,
      goldReward: 34500,
      skills: [
        { name: '천둥 포효', cost: 390, type: 'attack_magic_crit', power: 3.15, shockChance: 0.7 },
        { name: '전하 붕괴', cost: 360, type: 'attack_magic_stun', power: 3.0, stunChance: 0.18 }
      ]
    }
  },
  {
    id: 'plague_garden',
    name: '역병 정원',
    recommendedLevel: 124,
    description: '아름다운 꽃향기에 독과 출혈이 숨어 있는 정원입니다. 오래 끌수록 지속 피해가 쌓입니다.',
    bossCleared: false,
    floors: 10,
    monsters: [
      {
        name: '가시 만드라고라',
        icon: '🌺',
        maxHp: 20400,
        maxMp: 2600,
        physicalAttack: 1760,
        magicAttack: 980,
        defense: 1080,
        speed: 48,
        critRate: 0.24,
        xpReward: 26000,
        goldReward: 5600,
        skills: [
          { name: '가시 분출', cost: 300, type: 'attack_physical', power: 3.05, bleedChance: 0.65 }
        ]
      },
      {
        name: '역병 향기술사',
        icon: '🧪',
        maxHp: 18800,
        maxMp: 4400,
        physicalAttack: 980,
        magicAttack: 1880,
        defense: 980,
        speed: 52,
        critRate: 0.25,
        xpReward: 26800,
        goldReward: 5800,
        skills: [
          { name: '녹색 향무', cost: 320, type: 'attack_magic_poison', power: 3.0, poisonChance: 0.78, weakenChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '만개의 역병왕 베르단트',
      icon: '🌿',
      maxHp: 89000,
      maxMp: 11600,
      physicalAttack: 1850,
      magicAttack: 2120,
      defense: 1260,
      speed: 52,
      critRate: 0.29,
      xpReward: 210000,
      goldReward: 39000,
      skills: [
        { name: '검은 개화', cost: 430, type: 'attack_magic_poison', power: 3.25, poisonChance: 0.85, bleedChance: 0.45 },
        { name: '왕의 재생', cost: 380, type: 'heal_shield', power: 0.55, selfStatus: { type: 'regen', duration: 2, valueScale: 'magic', power: 0.18 } }
      ]
    }
  },
  {
    id: 'mirrored_bastion',
    name: '거울 보루',
    recommendedLevel: 132,
    description: '거울벽이 공격의 방향감각을 빼앗는 보루입니다. 실명과 취약, 침묵이 전투 흐름을 흔듭니다.',
    bossCleared: false,
    floors: 10,
    monsters: [
      {
        name: '반사 검객',
        icon: '🪞',
        maxHp: 22600,
        maxMp: 3200,
        physicalAttack: 2050,
        magicAttack: 1100,
        defense: 1320,
        speed: 60,
        critRate: 0.31,
        xpReward: 30000,
        goldReward: 6400,
        skills: [
          { name: '반사 베기', cost: 330, type: 'attack_physical_crit', power: 3.1, vulnerableChance: 0.4 }
        ]
      },
      {
        name: '침묵의 거울마녀',
        icon: '🧙',
        maxHp: 21000,
        maxMp: 5200,
        physicalAttack: 980,
        magicAttack: 2280,
        defense: 1180,
        speed: 56,
        critRate: 0.27,
        xpReward: 31200,
        goldReward: 6600,
        skills: [
          { name: '무음 반사', cost: 360, type: 'attack_magic', power: 3.12, silenceChance: 0.38, blindChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '거울 군주 스페큘라',
      icon: '🔷',
      maxHp: 104000,
      maxMp: 13200,
      physicalAttack: 2150,
      magicAttack: 2460,
      defense: 1440,
      speed: 60,
      critRate: 0.33,
      xpReward: 250000,
      goldReward: 44000,
      skills: [
        { name: '만상 반전', cost: 470, type: 'attack_magic_crit', power: 3.35, blindChance: 0.55, vulnerableChance: 0.45 },
        { name: '거울 속 침묵', cost: 420, type: 'attack_magic', power: 3.1, silenceChance: 0.42 }
      ]
    }
  },
  {
    id: 'ashstorm_front',
    name: '잿폭풍 전선',
    recommendedLevel: 140,
    description: '타오른 전장이 끝없이 재가 되어 흩날립니다. 화상과 약화가 동시에 밀려옵니다.',
    bossCleared: false,
    floors: 10,
    monsters: [
      {
        name: '잿바람 포병',
        icon: '💣',
        maxHp: 25200,
        maxMp: 4600,
        physicalAttack: 1600,
        magicAttack: 2520,
        defense: 1380,
        speed: 54,
        critRate: 0.29,
        xpReward: 34800,
        goldReward: 7200,
        skills: [
          { name: '소이 포격', cost: 390, type: 'attack_magic', power: 3.35, burnChance: 0.82, weakenChance: 0.35 }
        ]
      },
      {
        name: '재의 돌격병',
        icon: '🛡️',
        maxHp: 27800,
        maxMp: 3400,
        physicalAttack: 2380,
        magicAttack: 920,
        defense: 1560,
        speed: 50,
        critRate: 0.26,
        xpReward: 35600,
        goldReward: 7400,
        skills: [
          { name: '그을린 방패벽', cost: 340, type: 'attack_physical', power: 3.22, weakenChance: 0.5 }
        ]
      }
    ],
    boss: {
      name: '잿폭풍 장군 이그라스',
      icon: '🔥',
      maxHp: 122000,
      maxMp: 14800,
      physicalAttack: 2500,
      magicAttack: 2680,
      defense: 1620,
      speed: 54,
      critRate: 0.32,
      xpReward: 296000,
      goldReward: 50000,
      skills: [
        { name: '전선 초토화', cost: 520, type: 'attack_magic_crit', power: 3.55, burnChance: 0.88 },
        { name: '잿빛 압박', cost: 460, type: 'attack_physical', power: 3.35, vulnerableChance: 0.55, weakenChance: 0.45 }
      ]
    }
  },
  {
    id: 'frozen_starfall',
    name: '얼어붙은 별낙하',
    recommendedLevel: 148,
    description: '추락한 별이 얼음 속에서 맥동하는 장소입니다. 빙결과 마나 연소가 전투 리듬을 끊습니다.',
    bossCleared: false,
    floors: 11,
    monsters: [
      {
        name: '서리 별조각',
        icon: '❄️',
        maxHp: 30000,
        maxMp: 5600,
        physicalAttack: 1400,
        magicAttack: 2920,
        defense: 1640,
        speed: 58,
        critRate: 0.3,
        xpReward: 40500,
        goldReward: 8200,
        skills: [
          { name: '빙성 파편', cost: 430, type: 'attack_magic', power: 3.5, freezeChance: 0.28, manaBurnChance: 0.35 }
        ]
      },
      {
        name: '오한 관측자',
        icon: '👁️',
        maxHp: 28400,
        maxMp: 6200,
        physicalAttack: 1280,
        magicAttack: 3060,
        defense: 1520,
        speed: 62,
        critRate: 0.31,
        xpReward: 41800,
        goldReward: 8400,
        skills: [
          { name: '절대영도 시선', cost: 460, type: 'attack_magic', power: 3.45, freezeChance: 0.32, blindChance: 0.4 }
        ]
      }
    ],
    boss: {
      name: '얼어붙은 혜성 이스카론',
      icon: '☄️',
      maxHp: 146000,
      maxMp: 16600,
      physicalAttack: 2350,
      magicAttack: 3240,
      defense: 1800,
      speed: 62,
      critRate: 0.34,
      xpReward: 350000,
      goldReward: 57000,
      skills: [
        { name: '혜성 정지', cost: 560, type: 'attack_magic_crit', power: 3.75, freezeChance: 0.38 },
        { name: '별빛 연소', cost: 500, type: 'attack_magic', power: 3.45, manaBurnChance: 0.55 }
      ]
    }
  },
  {
    id: 'serpent_scriptorium',
    name: '뱀문자 기록원',
    recommendedLevel: 156,
    description: '살아 움직이는 문자가 독과 침묵의 주문을 새기는 기록원입니다.',
    bossCleared: false,
    floors: 11,
    monsters: [
      {
        name: '문자 독사',
        icon: '🐍',
        maxHp: 33800,
        maxMp: 5400,
        physicalAttack: 2600,
        magicAttack: 1900,
        defense: 1720,
        speed: 70,
        critRate: 0.34,
        xpReward: 46200,
        goldReward: 9200,
        skills: [
          { name: '독문 각인', cost: 460, type: 'attack_physical_poison', power: 3.55, poisonChance: 0.85, silenceChance: 0.28 }
        ]
      },
      {
        name: '봉인 서기관',
        icon: '📜',
        maxHp: 32000,
        maxMp: 7200,
        physicalAttack: 1320,
        magicAttack: 3420,
        defense: 1680,
        speed: 58,
        critRate: 0.3,
        xpReward: 47500,
        goldReward: 9400,
        skills: [
          { name: '봉인 문장', cost: 500, type: 'attack_magic', power: 3.62, silenceChance: 0.48, vulnerableChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '고문서의 뱀왕 세르펜',
      icon: '🐍',
      maxHp: 172000,
      maxMp: 19000,
      physicalAttack: 2950,
      magicAttack: 3560,
      defense: 1980,
      speed: 66,
      critRate: 0.35,
      xpReward: 420000,
      goldReward: 65000,
      skills: [
        { name: '왕의 봉인문', cost: 620, type: 'attack_magic_crit', power: 3.85, silenceChance: 0.52 },
        { name: '독사 성문', cost: 580, type: 'attack_physical_poison', power: 3.75, poisonChance: 0.9, bleedChance: 0.45 }
      ]
    }
  },
  {
    id: 'eclipse_spire',
    name: '일식 첨탑',
    recommendedLevel: 164,
    description: '태양과 달이 겹친 그림자 첨탑입니다. 실명, 취약, 흡혈성 공격이 이어집니다.',
    bossCleared: false,
    floors: 11,
    monsters: [
      {
        name: '일식 추적자',
        icon: '🌘',
        maxHp: 36800,
        maxMp: 5800,
        physicalAttack: 3200,
        magicAttack: 1980,
        defense: 1960,
        speed: 78,
        critRate: 0.38,
        xpReward: 52500,
        goldReward: 10400,
        skills: [
          { name: '그림자 추적', cost: 520, type: 'attack_physical_crit', power: 3.72, blindChance: 0.45 }
        ]
      },
      {
        name: '흑일 마도사',
        icon: '🌑',
        maxHp: 35000,
        maxMp: 8400,
        physicalAttack: 1420,
        magicAttack: 3860,
        defense: 1840,
        speed: 64,
        critRate: 0.34,
        xpReward: 53800,
        goldReward: 10600,
        skills: [
          { name: '흑태양 낙인', cost: 560, type: 'attack_magic', power: 3.82, vulnerableChance: 0.55, blindChance: 0.35 }
        ]
      }
    ],
    boss: {
      name: '일식의 성녀 노크티아',
      icon: '🌒',
      maxHp: 205000,
      maxMp: 22000,
      physicalAttack: 3250,
      magicAttack: 4100,
      defense: 2200,
      speed: 72,
      critRate: 0.38,
      xpReward: 500000,
      goldReward: 74000,
      skills: [
        { name: '개기일식', cost: 680, type: 'attack_magic_crit', power: 4.05, blindChance: 0.65, vulnerableChance: 0.5 },
        { name: '어둠의 성가', cost: 610, type: 'heal_shield', power: 0.6, selfStatus: { type: 'regen', duration: 2, valueScale: 'magic', power: 0.2 } }
      ]
    }
  },
  {
    id: 'primal_maelstrom',
    name: '태초 소용돌이',
    recommendedLevel: 172,
    description: '사원소가 한꺼번에 뒤엉킨 원초의 소용돌이입니다. 모든 상태이상이 복합적으로 등장합니다.',
    bossCleared: false,
    floors: 12,
    monsters: [
      {
        name: '사원소 포식체',
        icon: '🌈',
        maxHp: 42000,
        maxMp: 9000,
        physicalAttack: 2800,
        magicAttack: 4300,
        defense: 2240,
        speed: 70,
        critRate: 0.36,
        xpReward: 62000,
        goldReward: 12200,
        skills: [
          { name: '원소 난류', cost: 640, type: 'attack_magic_crit', power: 4.0, burnChance: 0.55, shockChance: 0.55, blindChance: 0.35 }
        ]
      },
      {
        name: '원초의 파수핵',
        icon: '🜁',
        maxHp: 46000,
        maxMp: 7600,
        physicalAttack: 3900,
        magicAttack: 2800,
        defense: 2520,
        speed: 58,
        critRate: 0.34,
        xpReward: 63800,
        goldReward: 12600,
        skills: [
          { name: '핵심 압괴', cost: 620, type: 'attack_physical', power: 4.0, weakenChance: 0.55, bleedChance: 0.45 }
        ]
      }
    ],
    boss: {
      name: '원초폭풍 칼라미타스',
      icon: '🌀',
      maxHp: 245000,
      maxMp: 26000,
      physicalAttack: 4100,
      magicAttack: 4650,
      defense: 2600,
      speed: 74,
      critRate: 0.4,
      xpReward: 610000,
      goldReward: 86000,
      skills: [
        { name: '사원소 붕괴', cost: 760, type: 'attack_magic_crit', power: 4.3, burnChance: 0.65, shockChance: 0.65, freezeChance: 0.25 },
        { name: '태초의 압력', cost: 700, type: 'attack_physical', power: 4.15, vulnerableChance: 0.55, weakenChance: 0.55 }
      ]
    }
  },
  {
    id: 'aetheria_core',
    name: '에테리아 심장부',
    recommendedLevel: 180,
    description: '모든 던전과 차원을 움직이는 심장입니다. 최종 수호자가 영웅의 완성도를 시험합니다.',
    bossCleared: false,
    floors: 12,
    monsters: [
      {
        name: '심장부 맥동체',
        icon: '💠',
        maxHp: 50000,
        maxMp: 10400,
        physicalAttack: 3600,
        magicAttack: 5100,
        defense: 2700,
        speed: 76,
        critRate: 0.39,
        xpReward: 75000,
        goldReward: 14500,
        skills: [
          { name: '에테르 맥동', cost: 760, type: 'attack_magic_crit', power: 4.35, manaBurnChance: 0.55, shockChance: 0.5 }
        ]
      },
      {
        name: '완성의 집행자',
        icon: '👑',
        maxHp: 56000,
        maxMp: 9200,
        physicalAttack: 5000,
        magicAttack: 3200,
        defense: 3000,
        speed: 68,
        critRate: 0.38,
        xpReward: 78000,
        goldReward: 15000,
        skills: [
          { name: '완성의 참격', cost: 720, type: 'attack_physical_crit', power: 4.35, bleedChance: 0.6, vulnerableChance: 0.45 }
        ]
      }
    ],
    boss: {
      name: '에테리아의 심장 아르카이온',
      icon: '💎',
      maxHp: 320000,
      maxMp: 32000,
      physicalAttack: 5200,
      magicAttack: 5600,
      defense: 3300,
      speed: 78,
      critRate: 0.42,
      xpReward: 800000,
      goldReward: 120000,
      skills: [
        { name: '세계 심장 박동', cost: 900, type: 'attack_magic_crit', power: 4.7, shockChance: 0.7, manaBurnChance: 0.55 },
        { name: '종언의 궤도', cost: 840, type: 'attack_physical_crit', power: 4.55, bleedChance: 0.7, vulnerableChance: 0.55 },
        { name: '창세 재기동', cost: 780, type: 'heal_shield', power: 0.65, selfStatus: { type: 'regen', duration: 2, valueScale: 'magic', power: 0.22 } }
      ]
    }
  }
];

const LATE_STAGE_CONFIGS = [
  { id: 'silver_moon_archive', name: '은월 기록성', level: 188, theme: '실명, 침묵, 마나 연소', boss: '은월 사서 셀레네' },
  { id: 'ashen_clocktower', name: '잿빛 시계탑', level: 196, theme: '감전, 출혈, 취약', boss: '잿빛 시계공 오르로크' },
  { id: 'crystal_abyss', name: '수정 심연', level: 204, theme: '빙결, 취약, 마법 피해', boss: '심연 수정체 네레이드' },
  { id: 'silent_sun_temple', name: '무음 태양사원', level: 212, theme: '침묵, 실명, 약화', boss: '무음의 태양 라하르' },
  { id: 'eclipse_colosseum', name: '월식 투기장', level: 220, theme: '치명타, 기절, 출혈', boss: '월식 군주 발테온' },
  { id: 'seraphic_ruins', name: '천사 잔해지', level: 230, theme: '실명, 재생, 취약', boss: '부서진 세라프 아제르' },
  { id: 'thousand_venom_vault', name: '천독 저장고', level: 240, theme: '중독, 출혈, 마나 연소', boss: '천독 군주 모르바인' },
  { id: 'stormglass_sea', name: '폭풍유리 해역', level: 250, theme: '감전, 빙결, 침묵', boss: '유리폭풍 리바이아' },
  { id: 'red_mirror_maze', name: '붉은 거울미궁', level: 260, theme: '실명, 취약, 치명타', boss: '붉은 거울왕 비트라' },
  { id: 'infinite_constellation', name: '무한성좌', level: 270, theme: '복합 상태이상, 고마나전', boss: '무한성좌 아스트라' },
  { id: 'black_lotus_sanctum', name: '흑련 성역', level: 282, theme: '침묵, 약화, 보호막', boss: '흑련의 성자 나르키온' },
  { id: 'primordial_bellows', name: '태고 용광로', level: 294, theme: '화상, 감전, 취약', boss: '태고 대장장이 브루칸' },
  { id: 'void_orchestra', name: '공허 관현당', level: 306, theme: '실명, 침묵, 마나 연소', boss: '공허 지휘자 오르페온' },
  { id: 'crownless_throne', name: '무관의 왕좌', level: 318, theme: '기절, 출혈, 약화', boss: '무관왕 레갈리온' },
  { id: 'root_of_origin', name: '근원의 뿌리', level: 330, theme: '최종 복합 상태이상', boss: '근원의 왕 엘라드리온' }
];

function makeLateMonster(stageNumber, config, offset) {
  const scale = stageNumber - 30 + offset * 0.18;
  const magicLean = offset % 2 === 0;
  const baseHp = 62000 + scale * 15500;
  const baseAtk = 5200 + scale * 1180;
  const baseDef = 3400 + scale * 760;
  const cost = 820 + scale * 74;

  return {
    name: `${config.name} 파수체 ${offset + 1}`,
    icon: magicLean ? '🔮' : '⚔️',
    maxHp: Math.round(baseHp * (1 + offset * 0.1)),
    maxMp: Math.round(12000 + scale * 1500),
    physicalAttack: Math.round(magicLean ? baseAtk * 0.75 : baseAtk),
    magicAttack: Math.round(magicLean ? baseAtk : baseAtk * 0.72),
    defense: Math.round(baseDef),
    speed: Math.round(80 + scale * 1.7 + offset * 4),
    critRate: 0.36 + offset * 0.02,
    xpReward: Math.round(90000 + scale * 18000),
    goldReward: Math.round(17000 + scale * 2900),
    skills: [
      magicLean
        ? { name: '차원 봉인', cost: Math.round(cost), type: 'attack_magic', power: 4.4 + offset * 0.12, silenceChance: 0.38, blindChance: 0.32 }
        : { name: '근원 절단', cost: Math.round(cost), type: 'attack_physical_crit', power: 4.35 + offset * 0.14, bleedChance: 0.58, vulnerableChance: 0.38 }
    ]
  };
}

function makeLateBoss(stageNumber, config) {
  const scale = stageNumber - 30;
  return {
    name: config.boss,
    icon: stageNumber % 5 === 0 ? '👑' : '💠',
    maxHp: Math.round(360000 + scale * 98000),
    maxMp: Math.round(36000 + scale * 8600),
    physicalAttack: Math.round(5900 + scale * 1650),
    magicAttack: Math.round(6200 + scale * 1720),
    defense: Math.round(3800 + scale * 1100),
    speed: Math.round(84 + scale * 2.2),
    critRate: 0.42,
    xpReward: Math.round(900000 + scale * 260000),
    goldReward: Math.round(140000 + scale * 42000),
    skills: [
      { name: '왕권 붕괴', cost: Math.round(980 + scale * 95), type: 'attack_physical_crit', power: 4.7 + scale * 0.05, stunChance: 0.22, vulnerableChance: 0.52 },
      { name: '근원 주문', cost: Math.round(1040 + scale * 100), type: 'attack_magic_crit', power: 4.85 + scale * 0.055, blindChance: 0.5, silenceChance: 0.42 },
      { name: '차원 재생', cost: Math.round(900 + scale * 85), type: 'heal_shield', power: 0.6, selfStatus: { type: 'regen', duration: 2, valueScale: 'magic', power: 0.18 } }
    ]
  };
}

LATE_STAGE_CONFIGS.forEach((config, index) => {
  const stageNumber = 31 + index;
  STAGES.push({
    id: config.id,
    name: config.name,
    recommendedLevel: config.level,
    description: `${config.theme} 중심의 후반 차원 던전입니다. 기존 던전보다 높은 등급 장비가 등장합니다.`,
    bossCleared: false,
    floors: stageNumber < 36 ? 12 : stageNumber < 41 ? 13 : 14,
    monsters: [
      makeLateMonster(stageNumber, config, 0),
      makeLateMonster(stageNumber, config, 1),
      makeLateMonster(stageNumber, config, 2)
    ],
    boss: makeLateBoss(stageNumber, config)
  });
});

// Bind to window
window.STAGES = STAGES;
