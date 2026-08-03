// Combat Engine Module (Global scope)

class CombatManager {
  constructor(game) {
    this.game = game;
    this.active = false;
    
    this.player = null; // Reference to player character
    this.enemy = null;  // Fighter instance for monster
    this.stage = null;
    this.currentFloor = 1;
    this.isBossFloor = false;
    
    this.turn = 'player'; // 'player' or 'enemy'
    this.turnCount = 1;
    this.enemyManaRegenTurnCounter = 0;
    this.combatEnding = false;
    this.playerSilencedThisTurn = false;
    
    // UI elements references
    this.ui = {
      screen: document.getElementById('screen-combat'),
      stageName: document.querySelector('.combat-stage-name'),
      turnIndicator: document.querySelector('.combat-turn-indicator'),
      exitDungeonBtn: document.getElementById('btn-exit-dungeon'),
      logBox: document.querySelector('.combat-log-box'),
      actionsDeck: document.querySelector('.combat-actions-deck'),
      
      playerAvatar: document.getElementById('player-combat-avatar'),
      playerName: document.getElementById('player-combat-name'),
      playerHpBar: document.getElementById('player-combat-hp'),
      playerHpText: document.getElementById('player-combat-hp-text'),
      playerMpBar: document.getElementById('player-combat-mp'),
      playerMpText: document.getElementById('player-combat-mp-text'),
      playerStatus: document.getElementById('player-combat-status'),
      playerWrapper: document.getElementById('player-combat-wrapper'),
      
      enemyAvatar: document.getElementById('enemy-combat-avatar'),
      enemyName: document.getElementById('enemy-combat-name'),
      enemyHpBar: document.getElementById('enemy-combat-hp'),
      enemyHpText: document.getElementById('enemy-combat-hp-text'),
      enemyMpBar: document.getElementById('enemy-combat-mp'),
      enemyMpText: document.getElementById('enemy-combat-mp-text'),
      enemyStatus: document.getElementById('enemy-combat-status'),
      enemyWrapper: document.getElementById('enemy-combat-wrapper'),
      
      victoryModal: document.getElementById('modal-victory'),
      defeatModal: document.getElementById('modal-defeat')
    };

    if (this.ui.exitDungeonBtn) {
      this.ui.exitDungeonBtn.onclick = () => this.exitDungeon();
    }
  }

  startCombat(player, stage, currentFloor = 1) {
    this.active = true;
    this.combatEnding = false;
    this.player = player;
    this.stage = stage;
    this.currentFloor = currentFloor;
    this.isBossFloor = currentFloor === stage.floors;
    
    this.player.recalculateStats();
    this.player.clearStatusEffects();
    
    // Select monster
    let monsterData;
    if (this.isBossFloor) {
      monsterData = stage.boss;
      this.ui.stageName.textContent = `${stage.name} - 최종 보스전`;
    } else {
      monsterData = stage.monsters[Math.floor(Math.random() * stage.monsters.length)];
      this.ui.stageName.textContent = `${stage.name} - ${currentFloor}층`;
    }
    
    // Scale enemy stats slightly based on floor
    const scale = 1 + (this.currentFloor - 1) * 0.12;
    const enemyStats = {
      maxHp: monsterData.maxHp * scale,
      maxMp: monsterData.maxMp * scale,
      physicalAttack: monsterData.physicalAttack * scale,
      magicAttack: monsterData.magicAttack * scale,
      defense: monsterData.defense * scale,
      speed: monsterData.speed * scale,
      critRate: monsterData.critRate
    };
    
    // Instantiate with global Fighter
    this.enemy = new window.Fighter(monsterData.name, 'monster', enemyStats, player.level);
    this.enemy.icon = monsterData.icon;
    const monsterCostMultiplier = window.MONSTER_SKILL_MANA_COST_MULTIPLIER ?? 1.35;
    this.enemy.skills = (monsterData.skills || []).map(skill => ({
      ...skill,
      cost: Math.ceil((skill.cost || 0) * monsterCostMultiplier)
    }));
    this.enemy.xpReward = Math.round(monsterData.xpReward * (this.isBossFloor ? 1 : scale));
    this.enemy.goldReward = Math.round(monsterData.goldReward * (this.isBossFloor ? 1 : scale));
    
    // Clear logs
    this.ui.logBox.innerHTML = '';
    this.addLog(`⚔️ **${this.enemy.name}**이(가) 나타났습니다! 전투가 시작됩니다.`, 'system');
    
    // Determine who goes first based on speed
    this.turnCount = 1;
    this.enemyManaRegenTurnCounter = 0;
    if (this.player.speed >= this.enemy.speed) {
      this.turn = 'player';
      this.addLog(`⚡ 선공 획득! 플레이어의 속도(${this.player.speed})가 적(${this.enemy.speed})보다 빠릅니다.`, 'system');
    } else {
      this.turn = 'enemy';
      this.addLog(`⚠️ 기습! 적의 속도(${this.enemy.speed})가 플레이어(${this.player.speed})보다 빠릅니다.`, 'system');
    }
    
    this.updateFightersUI();
    this.renderActionButtons();
    
    // If enemy turn, trigger enemy actions
    if (this.turn === 'enemy') {
      setTimeout(() => this.executeEnemyTurn(), 1000);
    }
  }

  scheduleCombatEnd(result, delay = 800) {
    if (this.combatEnding) return;

    this.combatEnding = true;
    this.active = false;
    this.ui.actionsDeck.innerHTML = '';
    this.game.updateKeyboardHints?.();

    setTimeout(() => {
      if (result === 'victory') {
        this.resolveVictory();
      } else {
        this.resolveDefeat();
      }
    }, delay);
  }

  exitDungeon() {
    if (!this.active || this.combatEnding) return;

    const ok = confirm('던전을 나가고 마을로 돌아가시겠습니까? 현재 전투 보상은 받을 수 없습니다.');
    if (!ok) return;

    this.combatEnding = true;
    this.active = false;
    this.ui.actionsDeck.innerHTML = '';
    this.game.updateKeyboardHints?.();
    this.player.clearStatusEffects();
    this.player.recalculateStats();
    this.game.addGeneralLog(`🚪 ${this.stage.name} 탐험을 중단하고 마을로 귀환했습니다.`);
    this.game.changeScreen('town');
    this.game.openTownPanel('adventure');
    this.game.saveGame();
  }

  addLog(message, type = '') {
    const entry = document.createElement('div');
    entry.className = `log-entry ${type ? 'log-' + type : ''}`;
    entry.innerHTML = message.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
    this.ui.logBox.appendChild(entry);
    this.ui.logBox.scrollTop = this.ui.logBox.scrollHeight;
  }

  updateFightersUI() {
    // Player
    this.ui.playerName.textContent = `${this.player.name} (Lv.${this.player.level})`;
    this.ui.playerHpBar.style.width = `${(this.player.hp / this.player.maxHp) * 100}%`;
    this.ui.playerHpText.textContent = `${this.player.hp} / ${this.player.maxHp}`;
    this.ui.playerMpBar.style.width = `${(this.player.mp / this.player.maxMp) * 100}%`;
    this.ui.playerMpText.textContent = `${this.player.mp} / ${this.player.maxMp}`;
    
    // Enemy
    this.ui.enemyAvatar.textContent = this.enemy.icon;
    this.ui.enemyName.textContent = `${this.enemy.name}`;
    this.ui.enemyHpBar.style.width = `${(this.enemy.hp / this.enemy.maxHp) * 100}%`;
    this.ui.enemyHpText.textContent = `${this.enemy.hp} / ${this.enemy.maxHp}`;
    this.ui.enemyMpBar.style.width = `${(this.enemy.mp / this.enemy.maxMp) * 100}%`;
    this.ui.enemyMpText.textContent = `${this.enemy.mp} / ${this.enemy.maxMp}`;

    if (this.turn === 'player') {
      this.ui.playerWrapper.classList.add('active-turn');
      this.ui.enemyWrapper.classList.remove('active-turn');
      this.ui.turnIndicator.textContent = '아군 턴';
      this.ui.turnIndicator.style.color = 'var(--secondary-color)';
    } else {
      this.ui.playerWrapper.classList.remove('active-turn');
      this.ui.enemyWrapper.classList.add('active-turn');
      this.ui.turnIndicator.textContent = '적군 턴';
      this.ui.turnIndicator.style.color = 'var(--danger-color)';
    }

    this.renderStatusEffects(this.player, this.ui.playerStatus);
    this.renderStatusEffects(this.enemy, this.ui.enemyStatus);
  }

  renderStatusEffects(fighter, container) {
    container.innerHTML = '';
    
    if (fighter.shield > 0) {
      const shieldBadge = document.createElement('span');
      shieldBadge.className = 'status-badge status-shield';
      shieldBadge.textContent = `🛡️ ${Math.round(fighter.shield)}`;
      container.appendChild(shieldBadge);
    }
    
    fighter.statusEffects.forEach(effect => {
      const badge = document.createElement('span');
      const definition = window.STATUS_EFFECT_DEFINITIONS?.[effect.type] || {
        name: effect.type,
        icon: '✦',
        className: 'status-generic'
      };
      badge.className = `status-badge ${definition.className}`;
      badge.textContent = `${definition.icon} ${definition.name}(${Math.max(1, effect.duration)})`;
      container.appendChild(badge);
    });
  }

  renderActionButtons() {
    this.ui.actionsDeck.innerHTML = '';
    
    if (this.turn !== 'player' || !this.active) {
      this.game.updateKeyboardHints?.();
      return;
    }

    const basicAttackType = window.getBasicAttackTypeForClass?.(this.player.classType) || 'physical';
    const basicBtn = document.createElement('button');
    basicBtn.className = 'btn action-btn';
    basicBtn.innerHTML = `${basicAttackType === 'magic' ? '🔮' : '⚔️'} 일반 공격 <span class="skill-cost">${basicAttackType === 'magic' ? '주문력' : '공격력'} · 마나 0</span>`;
    basicBtn.title = basicAttackType === 'magic'
      ? '주문력으로 피해를 주는 일반 공격입니다.'
      : '물리 공격력으로 피해를 주는 일반 공격입니다.';
    basicBtn.onclick = () => this.executePlayerAction('basic_attack');
    this.ui.actionsDeck.appendChild(basicBtn);
    
    // 2. Class Skills
    this.player.skills.forEach(skill => {
      const skillBtn = document.createElement('button');
      skillBtn.className = 'btn btn-primary action-btn';
      
      const isManaShort = this.player.mp < skill.cost;
      const isSilenced = this.playerSilencedThisTurn || this.player.hasStatusEffect?.('silence');
      skillBtn.disabled = isManaShort || isSilenced;
      
      skillBtn.innerHTML = `✨ ${skill.name} <span class="skill-cost">마나 ${skill.cost}</span>`;
      skillBtn.title = skill.description;
      skillBtn.onclick = () => this.executePlayerAction('skill', skill);
      this.ui.actionsDeck.appendChild(skillBtn);
    });

    // Potions are town-only; combat actions stay focused on attacks and class skills.
    this.game.updateKeyboardHints?.();
  }

  executePlayerAction(actionType, detail = null) {
    if (this.turn !== 'player' || !this.active) return;
    
    this.ui.actionsDeck.innerHTML = '';
    this.game.updateKeyboardHints?.();
    
    let delay = 1300;
    
    if (actionType === 'basic_attack') {
      const basicAttackType = window.getBasicAttackTypeForClass?.(this.player.classType) || 'physical';
      this.resolveAttack(this.player, this.enemy, 1.0, basicAttackType);
    } else if (actionType === 'focus') {
      const classDetail = window.CLASS_DETAILS[this.player.classType];
      const focusAction = classDetail?.focusAction || {
        name: '전투 집중',
        icon: '✨',
        mpRate: 0.25,
        message: '전투 태세를 가다듬어 마나를 회복했습니다.'
      };
      const restored = this.player.restoreMp(Math.round(this.player.maxMp * (focusAction.mpRate || 0.25)));
      if (focusAction.status) {
        const status = focusAction.status;
        const value = this.getScaledSelfStatusValue(this.player, status);
        this.player.addStatusEffect(status.type, status.duration || 1, value);
      }
      this.addLog(`${focusAction.icon} **${this.player.name}**이(가) ${focusAction.message} (마나 **${restored}**)`, 'heal');
      this.createFloatingText(`MP +${restored}`, 'text', true);
      delay = 900;
    } else if (actionType === 'skill') {
      this.player.mp -= detail.cost;
      this.addLog(`✨ **${this.player.name}**이(가) 마나 **${detail.cost}**을 소모하여 [**${detail.name}**] 발동!`, 'player');
      this.resolveSkillEffect(this.player, this.enemy, detail);
    }

    this.updateFightersUI();

    if (this.enemy.hp <= 0) {
      this.scheduleCombatEnd('victory', 1000);
      return;
    }

    this.player.expireActionStatusEffects?.();
    this.playerSilencedThisTurn = false;
    this.turn = 'enemy';
    setTimeout(() => {
      this.executeEnemyTurn();
    }, delay);
  }

  resolveAttack(attacker, defender, power, type = 'physical', bonusCrit = 0, options = {}) {
    const isPlayer = attacker === this.player;
    this.triggerAvatarAnimation(isPlayer, isPlayer ? 'melee-strike-right' : 'melee-strike-left');

    const blindMissChance = attacker.getStatusValue?.('blind', 0) || 0;
    if (blindMissChance && Math.random() < blindMissChance) {
      setTimeout(() => {
        if (!this.active) return;
        this.addLog(`🌑 **${attacker.name}**이(가) 실명 때문에 공격을 빗맞혔습니다!`, isPlayer ? 'player' : 'enemy');
        this.createFloatingText('MISS', 'text', !isPlayer);
        this.updateFightersUI();
      }, 200);
      return { missed: true };
    }

    const defenderEvasion = typeof defender.getEffectiveEvasion === 'function'
      ? defender.getEffectiveEvasion()
      : Math.min(defender.evasion || 0, 0.8);
    if (defenderEvasion && Math.random() < defenderEvasion) {
      setTimeout(() => {
        if (!this.active) return;
        this.addLog(`💨 **${defender.name}**이(가) 공격을 회피했습니다!`, defender === this.player ? 'player' : 'enemy');
        this.createFloatingText('MISS', 'text', defender === this.player);
        this.updateFightersUI();
      }, 200);
      return { missed: true };
    }
    
    const baseStat = type === 'magic' ? attacker.magicAttack : attacker.physicalAttack;
    const defenderDefense = typeof defender.getEffectiveDefense === 'function'
      ? defender.getEffectiveDefense()
      : defender.defense;
    const defenseDampening = 100 / (100 + defenderDefense);
    let dmg = baseStat * power * defenseDampening;

    const isBurned = defender.statusEffects.some(e => e.type === 'burn');
    if (isBurned) {
      dmg *= 1.25;
    }
    if (attacker.hasStatusEffect?.('weaken')) {
      dmg *= 1 - attacker.getStatusValue('weaken', 0.25);
    }
    if (attacker.hasStatusEffect?.('damage_boost')) {
      dmg *= 1 + attacker.getStatusValue('damage_boost', 0.2);
    }
    if (defender.hasStatusEffect?.('vulnerable')) {
      dmg *= 1 + defender.getStatusValue('vulnerable', 0.2);
    }
    if (defender.hasStatusEffect?.('freeze')) {
      dmg *= 1.15;
    }
    
    const isCrit = options.forceCrit || Math.random() < (attacker.critRate + bonusCrit);
    if (isCrit) {
      dmg *= 1 + (attacker.critDamage ?? 0.5);
    }

    if (defender.damageReduction) {
      dmg *= 1 - Math.min(defender.damageReduction, 0.60);
    }
    
    dmg = Math.round(dmg);
    if (dmg < 1) dmg = 1;
    
    setTimeout(() => {
      if (!this.active) return;
      const hadShield = defender.shield > 0;
      const taken = defender.takeDamage(dmg);
      this.triggerAvatarAnimation(!isPlayer, 'damage-shake');
      
      let critTag = isCrit ? '💥 **치명타!** ' : '';
      let shieldText = hadShield && taken < dmg ? ` (보호막 흡수 ${dmg - taken})` : '';
      
      this.addLog(`${critTag}**${attacker.name}**의 공격! **${defender.name}**에게 **${taken}**의 피해${shieldText}.`, isPlayer ? 'player' : 'enemy');
      if (taken > 0) {
        this.createFloatingText(taken, isCrit ? 'crit' : 'damage', !isPlayer);
      } else {
        this.createFloatingText('BLOCK', 'text', !isPlayer);
      }

      if (options.lifeStealRatio && taken > 0) {
        const healed = attacker.heal(Math.round(taken * options.lifeStealRatio));
        if (healed > 0) {
          this.addLog(`🩸 **${attacker.name}**이(가) 기술의 흡혈 효과로 **${healed}** 회복했습니다.`, isPlayer ? 'heal' : 'enemy');
          this.createFloatingText(healed, 'heal', isPlayer);
        }
      }

      if (attacker.lifeSteal && taken > 0) {
        const healed = attacker.heal(Math.round(taken * attacker.lifeSteal));
        if (healed > 0) {
          this.addLog(`🩸 **${attacker.name}**이(가) 피해의 일부를 흡수해 **${healed}** 회복했습니다.`, isPlayer ? 'heal' : 'enemy');
          this.createFloatingText(healed, 'heal', isPlayer);
        }
      }

      this.updateFightersUI();

      if (defender.hp <= 0) {
        this.scheduleCombatEnd(defender === this.enemy ? 'victory' : 'defeat', 700);
      }
    }, 200);

    return { missed: false };
  }

  getScaledSelfStatusValue(attacker, status) {
    if (typeof status.value === 'number') return status.value;

    const baseStat = status.valueScale === 'defense'
      ? attacker.defense
      : status.valueScale === 'physical'
        ? attacker.physicalAttack
        : attacker.magicAttack;

    return Math.round(baseStat * (status.power || 0.2) * (1 + (attacker.statusPower || 0)));
  }

  applySelfStatus(attacker, skill, isPlayer) {
    if (!skill.selfStatus) return;

    const status = skill.selfStatus;
    const value = this.getScaledSelfStatusValue(attacker, status);
    attacker.addStatusEffect(status.type, status.duration || 2, value);
    const definition = window.STATUS_EFFECT_DEFINITIONS?.[status.type];
    this.addLog(`${definition?.icon || '✦'} **${attacker.name}**이(가) ${definition?.name || status.type} 효과를 얻었습니다.`, isPlayer ? 'player' : 'enemy');
    this.updateFightersUI();
  }

  getMonsterShieldRate() {
    const dungeonNumber = this.game.stages.findIndex(stage => stage.id === this.stage.id) + 1;
    return dungeonNumber <= 6 ? 0.20 : 0.05;
  }

  getMonsterShieldValue(monster) {
    const dungeonNumber = this.game.stages.findIndex(stage => stage.id === this.stage.id) + 1;
    const shieldValue = monster.maxHp * this.getMonsterShieldRate();
    return dungeonNumber >= 10 ? Math.min(shieldValue, 20000) : shieldValue;
  }

  resolveSkillEffect(attacker, defender, skill) {
    const isPlayer = attacker === this.player;

    if (skill.type.startsWith('attack')) {
      const dmgType = skill.type.includes('magic') ? 'magic' : 'physical';
      const bonusCrit = skill.type.includes('crit') ? 0.40 : 0;
      
      const attackResult = this.resolveAttack(attacker, defender, skill.power, dmgType, bonusCrit, {
        forceCrit: skill.forceCrit === true,
        lifeStealRatio: skill.lifeStealRatio || 0
      });

      if (attackResult?.missed) return;
      
      if (skill.stunChance && Math.random() < skill.stunChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          defender.addStatusEffect('stun', 1);
          this.addLog(`💫 **${defender.name}**이(가) 기절에 걸려 다음 턴에 행동할 수 없습니다!`, 'system');
          this.updateFightersUI();
        }, 300);
      }
      
      if (skill.burnChance && Math.random() < skill.burnChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const burnDmg = Math.round(attacker.magicAttack * 0.4 * (1 + (attacker.statusPower || 0)));
          defender.addStatusEffect('burn', 3, burnDmg);
          this.addLog(`🔥 **${defender.name}**이(가) 불길에 휩싸였습니다! (지속 피해 증가 및 화상 피해)`, 'system');
          this.updateFightersUI();
        }, 350);
      }
      
      if (skill.poisonChance && Math.random() < skill.poisonChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const poisonDmg = Math.round(attacker.physicalAttack * 0.3 * (1 + (attacker.statusPower || 0)));
          defender.addStatusEffect('poison', 3, poisonDmg);
          this.addLog(`🤢 **${defender.name}**이(가) 맹독에 중독되었습니다!`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.bleedChance && Math.random() < skill.bleedChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const bleedDmg = Math.round(attacker.physicalAttack * 0.28 * (1 + (attacker.statusPower || 0)));
          defender.addStatusEffect('bleed', 3, bleedDmg);
          this.addLog(`🩸 **${defender.name}**이(가) 출혈 상태가 되었습니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.shockChance && Math.random() < skill.shockChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const shockDmg = Math.round(attacker.magicAttack * 0.32 * (1 + (attacker.statusPower || 0)));
          defender.addStatusEffect('shock', 2, shockDmg);
          this.addLog(`⚡ **${defender.name}**이(가) 감전되었습니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.freezeChance && Math.random() < skill.freezeChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          defender.addStatusEffect('freeze', 1, 0);
          this.addLog(`❄️ **${defender.name}**이(가) 빙결되어 다음 행동이 막힙니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.blindChance && Math.random() < skill.blindChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          defender.addStatusEffect('blind', 2, 0.25);
          this.addLog(`🌑 **${defender.name}**이(가) 실명 상태가 되었습니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.weakenChance && Math.random() < skill.weakenChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          defender.addStatusEffect('weaken', 2, 0.25);
          this.addLog(`⬇️ **${defender.name}**이(가) 약화되어 주는 피해가 감소합니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.vulnerableChance && Math.random() < skill.vulnerableChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          defender.addStatusEffect('vulnerable', 2, 0.2);
          this.addLog(`🎯 **${defender.name}**이(가) 취약 상태가 되어 받는 피해가 증가합니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.silenceChance && Math.random() < skill.silenceChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const duration = skill.silenceDuration || 1;
          defender.addStatusEffect('silence', duration, 0);
          this.addLog(`🔇 **${defender.name}**이(가) ${duration}턴 동안 침묵하여 스킬을 사용할 수 없습니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.manaBurnChance && Math.random() < skill.manaBurnChance) {
        setTimeout(() => {
          if (!this.active || defender.hp <= 0) return;
          const manaBurn = Math.round(attacker.magicAttack * 0.25 * (1 + (attacker.statusPower || 0)));
          defender.addStatusEffect('mana_burn', 2, manaBurn);
          this.addLog(`💙 **${defender.name}**이(가) 마나 연소 상태가 되었습니다.`, 'system');
          this.updateFightersUI();
        }, 350);
      }

      if (skill.selfShieldPower) {
        const shieldVal = attacker.maxHp * skill.selfShieldPower;
        attacker.shield += shieldVal;
        this.addLog(`🛡️ **${attacker.name}**이(가) 보호막(**${Math.round(shieldVal)}**)을 얻었습니다.`, isPlayer ? 'player' : 'enemy');
        this.createFloatingText('SHIELD', 'text', isPlayer);
      }

      this.applySelfStatus(attacker, skill, isPlayer);
    } else if (skill.type === 'shield') {
      this.triggerAvatarAnimation(isPlayer, isPlayer ? 'melee-strike-right' : 'melee-strike-left');
      const shieldVal = isPlayer ? attacker.maxHp * skill.power : this.getMonsterShieldValue(attacker);
      attacker.shield += shieldVal;
      this.addLog(`🛡️ **${attacker.name}**이(가) 보호막(**${Math.round(shieldVal)}**)을 형성했습니다.`, isPlayer ? 'player' : 'enemy');
      this.createFloatingText('SHIELD', 'text', isPlayer);
      this.applySelfStatus(attacker, skill, isPlayer);
    } else if (skill.type === 'heal_shield') {
      this.triggerAvatarAnimation(isPlayer, isPlayer ? 'melee-strike-right' : 'melee-strike-left');
      const healAmount = attacker.magicAttack * (skill.power || 1.0);
      const healed = attacker.heal(healAmount);
      const shieldVal = isPlayer ? attacker.maxHp * Math.min(skill.power || 0.35, 0.65) : this.getMonsterShieldValue(attacker);
      attacker.shield += shieldVal;
      
      this.addLog(`🛡️ **${attacker.name}**이(가) 체력 **${healed}**을 회복하고 보호막 **${Math.round(shieldVal)}**을 얻었습니다.`, isPlayer ? 'player' : 'enemy');
      this.createFloatingText(healed, 'heal', isPlayer);
      this.applySelfStatus(attacker, skill, isPlayer);
    }
  }

  executeEnemyTurn() {
    if (!this.active || this.enemy.hp <= 0) return;
    
    const { dots, hasStun, hasFreeze, hasSilence, regen, manaBurn } = this.enemy.tickStatusEffects();
    this.updateFightersUI();

    if (dots.poison > 0) {
      const taken = this.enemy.takeDamage(dots.poison);
      this.addLog(`🤢 맹독! **${this.enemy.name}**이(가) 독에 의해 **${taken}**의 지속 피해를 입었습니다.`, 'enemy');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', false);

      if (this.player.classType === window.CLASS_TYPES.ROGUE && taken > 0) {
        const healed = this.player.heal(taken);
        if (healed > 0) {
          this.addLog(`🩸 독이 파고든 만큼 **${healed}**의 체력을 흡수했습니다.`, 'heal');
          this.createFloatingText(healed, 'heal', true);
        }
      }

      this.updateFightersUI();
    }
    if (dots.burn > 0) {
      const taken = this.enemy.takeDamage(dots.burn);
      this.addLog(`🔥 화상! **${this.enemy.name}**이(가) 화염에 데어 **${taken}**의 지속 피해를 입었습니다.`, 'enemy');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', false);
      this.updateFightersUI();
    }
    if (dots.bleed > 0) {
      const taken = this.enemy.takeDamage(dots.bleed);
      this.addLog(`🩸 출혈! **${this.enemy.name}**이(가) **${taken}**의 지속 피해를 입었습니다.`, 'enemy');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', false);
      this.updateFightersUI();
    }
    if (dots.shock > 0) {
      const taken = this.enemy.takeDamage(dots.shock);
      this.addLog(`⚡ 감전! **${this.enemy.name}**이(가) **${taken}**의 지속 피해를 입었습니다.`, 'enemy');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', false);
      this.updateFightersUI();
    }
    if (regen > 0) {
      const healed = this.enemy.heal(regen);
      if (healed > 0) {
        this.addLog(`🌿 재생! **${this.enemy.name}**이(가) 체력을 **${healed}** 회복했습니다.`, 'enemy');
        this.createFloatingText(healed, 'heal', false);
      }
      this.updateFightersUI();
    }
    if (manaBurn > 0) {
      const lost = Math.min(this.enemy.mp, manaBurn);
      this.enemy.mp -= lost;
      this.addLog(`💙 마나 연소! **${this.enemy.name}**이(가) 마나 **${lost}**을 잃었습니다.`, 'enemy');
      this.updateFightersUI();
    }

    if (this.enemy.hp <= 0) {
      this.scheduleCombatEnd('victory', 800);
      return;
    }

    if (hasStun || hasFreeze) {
      this.addLog(`💫 **${this.enemy.name}**은(는) 행동 불능 상태라 행동을 건너뜁니다!`, 'system');
      this.enemy.expireActionStatusEffects?.();
      this.turn = 'player';
      setTimeout(() => {
        this.startPlayerTurn();
      }, 1000);
      return;
    }

    this.enemyManaRegenTurnCounter++;
    if (this.enemyManaRegenTurnCounter >= 3) {
      this.enemyManaRegenTurnCounter = 0;
      const monsterManaRegen = window.MONSTER_TURN_MANA_REGEN ?? 0.15;
      const enemyManaRegen = Math.round(this.enemy.maxMp * monsterManaRegen);
      const restored = this.enemy.restoreMp(enemyManaRegen);
      if (restored > 0) {
        this.addLog(`✨ **${this.enemy.name}**이(가) 3턴마다 차오르는 마나를 받아 **${restored}** 회복했습니다.`, 'enemy');
        this.updateFightersUI();
      }
    }

    let actionChosen = 'basic';
    let skillChosen = null;

    if (this.enemy.skills.length > 0) {
      const availableSkills = hasSilence ? [] : this.enemy.skills.filter(s => this.enemy.mp >= s.cost);
      if (hasSilence) {
        this.addLog(`🔇 **${this.enemy.name}**은(는) 침묵 상태라 스킬을 사용할 수 없습니다.`, 'system');
      }
      if (availableSkills.length > 0 && Math.random() > 0.4) {
        actionChosen = 'skill';
        skillChosen = availableSkills[Math.floor(Math.random() * availableSkills.length)];
      }
    }

    if (actionChosen === 'skill') {
      this.enemy.mp -= skillChosen.cost;
      this.addLog(`🔮 **${this.enemy.name}**이(가) [**${skillChosen.name}**] 시전! (마나 ${skillChosen.cost})`, 'enemy');
      this.resolveSkillEffect(this.enemy, this.player, skillChosen);
    } else {
      this.resolveAttack(this.enemy, this.player, 1.0, 'physical');
    }

    this.enemy.expireActionStatusEffects?.();
    this.updateFightersUI();

    if (this.player.hp <= 0) {
      this.scheduleCombatEnd('defeat', 1200);
      return;
    }

    this.turn = 'player';
    setTimeout(() => {
      this.startPlayerTurn();
    }, 1300);
  }

  startPlayerTurn() {
    if (!this.active || this.player.hp <= 0) return;
    
    const { dots, hasStun, hasFreeze, hasSilence, regen: statusRegen, manaBurn } = this.player.tickStatusEffects();
    this.playerSilencedThisTurn = hasSilence;
    
    this.player.recalculateStats();
    this.updateFightersUI();

    if (dots.poison > 0) {
      const taken = this.player.takeDamage(dots.poison);
      this.addLog(`🤢 중독! **${this.player.name}**이(가) 독에 의해 **${taken}**의 지속 피해를 입었습니다.`, 'player');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', true);
      this.updateFightersUI();
    }
    if (dots.burn > 0) {
      const taken = this.player.takeDamage(dots.burn);
      this.addLog(`🔥 화상! **${this.player.name}**이(가) 화상으로 **${taken}**의 지속 피해를 입었습니다.`, 'player');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', true);
      this.updateFightersUI();
    }
    if (dots.bleed > 0) {
      const taken = this.player.takeDamage(dots.bleed);
      this.addLog(`🩸 출혈! **${this.player.name}**이(가) **${taken}**의 지속 피해를 입었습니다.`, 'player');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', true);
      this.updateFightersUI();
    }
    if (dots.shock > 0) {
      const taken = this.player.takeDamage(dots.shock);
      this.addLog(`⚡ 감전! **${this.player.name}**이(가) **${taken}**의 지속 피해를 입었습니다.`, 'player');
      this.createFloatingText(taken > 0 ? taken : 'BLOCK', taken > 0 ? 'damage' : 'text', true);
      this.updateFightersUI();
    }
    if (statusRegen > 0) {
      const healed = this.player.heal(statusRegen);
      if (healed > 0) {
        this.addLog(`🌿 재생! **${this.player.name}**이(가) 체력을 **${healed}** 회복했습니다.`, 'heal');
        this.createFloatingText(healed, 'heal', true);
      }
      this.updateFightersUI();
    }
    if (manaBurn > 0) {
      const lost = Math.min(this.player.mp, manaBurn);
      this.player.mp -= lost;
      this.addLog(`💙 마나 연소! **${this.player.name}**이(가) 마나 **${lost}**을 잃었습니다.`, 'player');
      this.updateFightersUI();
    }

    if (this.player.hp <= 0) {
      this.scheduleCombatEnd('defeat', 800);
      return;
    }

    if (hasStun || hasFreeze) {
      this.addLog(`💫 **${this.player.name}**은(는) 행동 불능 상태라 턴을 넘깁니다!`, 'system');
      this.player.expireActionStatusEffects?.();
      this.playerSilencedThisTurn = false;
      this.turn = 'enemy';
      setTimeout(() => {
        this.executeEnemyTurn();
      }, 1000);
      return;
    }

    const baseManaRegen = window.BASE_TURN_MANA_REGEN ?? 0.075;
    const regen = Math.round(this.player.maxMp * (baseManaRegen + (this.player.manaRegen || 0)));
    this.player.restoreMp(regen);
    this.addLog(`✨ 턴 시작: 마나가 **${regen}** 회복되었습니다.`, 'system');
    if (this.playerSilencedThisTurn) {
      this.addLog(`🔇 **${this.player.name}**은(는) 침묵 상태라 이번 턴에 스킬을 사용할 수 없습니다.`, 'system');
    }
    
    this.turnCount++;
    this.updateFightersUI();
    this.renderActionButtons();
  }

  resolveVictory() {
    this.active = false;
    
    const xpEarned = this.enemy.xpReward;
    const goldEarned = this.enemy.goldReward;
    
    const lvUp = this.player.gainExp(xpEarned);
    this.player.gainGold(goldEarned);
    
    let lootDropped = null;
    const lootChance = this.isBossFloor ? 1.0 : Math.min(0.85, 0.35 + (this.player.itemFind || 0));
    const dungeonNumber = this.game.stages.findIndex(stage => stage.id === this.stage.id) + 1;
    
    if (Math.random() < lootChance) {
      let forcedRarity = null;
      if (this.isBossFloor && window.NAMED_BOSS_DUNGEONS?.[dungeonNumber] && Math.random() < 0.1) {
        lootDropped = window.generateNamedBossGear(this.player.classType, this.player.level, dungeonNumber);
      } else if (this.isBossFloor) {
        forcedRarity = window.getRandomBossRarity(dungeonNumber);
        lootDropped = window.generateRandomGear(this.player.classType, this.player.level, forcedRarity, dungeonNumber);
      } else {
        lootDropped = window.generateRandomGear(this.player.classType, this.player.level, forcedRarity, dungeonNumber);
      }
      if (lootDropped) this.game.inventory.push(lootDropped);
    }
    
    document.getElementById('vic-monster-name').textContent = this.enemy.name;
    document.getElementById('vic-xp-value').textContent = xpEarned;
    document.getElementById('vic-gold-value').textContent = goldEarned;
    
    const lvUpText = document.getElementById('vic-levelup-text');
    if (lvUp > 0) {
      lvUpText.textContent = `🎉 축하합니다! 레벨이 ${this.player.level}로 올랐습니다!`;
      lvUpText.style.display = 'block';
    } else {
      lvUpText.style.display = 'none';
    }
    
    const lootBox = document.getElementById('vic-loot-box');
    if (lootDropped) {
      lootBox.style.display = 'block';
      const nameSpan = document.getElementById('vic-loot-name');
      const descSpan = document.getElementById('vic-loot-desc');
      
      nameSpan.textContent = `[${lootDropped.rarity.name}] ${lootDropped.name}`;
      nameSpan.style.color = lootDropped.rarity.color;
      descSpan.textContent = lootDropped.description;
    } else {
      lootBox.style.display = 'none';
    }
    
    this.ui.victoryModal.classList.add('active');
  }

  resolveDefeat() {
    this.active = false;
    this.ui.defeatModal.classList.add('active');
  }

  claimVictory() {
    this.ui.victoryModal.classList.remove('active');
    
    if (this.isBossFloor) {
      this.stage.bossCleared = true;
      this.game.addGeneralLog(`🎉 ${this.stage.name}의 지배자를 토벌하고 던전을 정복했습니다!`);

      const isFinalStage = this.stage.id === this.game.stages[this.game.stages.length - 1].id;
      if (isFinalStage) {
        this.game.showGameClear();
      } else {
        this.game.changeScreen('town');
        this.game.openTownPanel('adventure');
        this.game.saveGame();
      }
    } else {
      this.currentFloor++;
      this.startCombat(this.player, this.stage, this.currentFloor);
    }
  }

  escapeDefeat() {
    this.ui.defeatModal.classList.remove('active');
    const penalty = Math.round(this.player.gold * 0.15);
    this.player.gold = Math.max(0, this.player.gold - penalty);
    this.player.hp = Math.round(this.player.maxHp * 0.3);
    this.player.mp = Math.round(this.player.maxMp * 0.3);
    
    this.game.addGeneralLog(`💀 전사했습니다. 치료비와 차원 탈출 비용으로 **${penalty}**골드가 소모되었습니다.`);
    this.game.changeScreen('town');
    this.game.openTownPanel('character');
    this.game.saveGame();
  }

  triggerAvatarAnimation(isPlayer, animClass) {
    const target = isPlayer ? this.ui.playerAvatar : this.ui.enemyAvatar;
    if (!target) return;
    
    target.classList.add(animClass);
    setTimeout(() => {
      target.classList.remove(animClass);
    }, 400);
  }

  createFloatingText(text, type, isPlayer) {
    const parent = isPlayer ? this.ui.playerWrapper : this.ui.enemyWrapper;
    if (!parent) return;

    const el = document.createElement('div');
    el.className = `floating-${type === 'crit' ? 'damage' : type} floating-text`;
    
    if (type === 'heal') el.textContent = `+${text}`;
    else if (type === 'damage') el.textContent = `-${text}`;
    else if (type === 'crit') el.textContent = `💥-${text}`;
    else el.textContent = text;
    
    const rx = Math.random() * 40 - 20;
    const ry = Math.random() * 40 - 20;
    
    el.style.left = `calc(50% + ${rx}px)`;
    el.style.top = `calc(40% + ${ry}px)`;
    
    parent.appendChild(el);
    setTimeout(() => el.remove(), 900);
  }
}

// Bind to window
window.CombatManager = CombatManager;
