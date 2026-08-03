// Main Game Manager (Global scope)

const SAVE_SLOT_COUNT = 7;
const SAVE_KEY_PREFIX = 'aetheria_rpg_save_slot_';
const LEGACY_SAVE_KEY = 'aetheria_rpg_save';

class GameManager {
  constructor() {
    this.player = null;
    this.inventory = [];
    this.stages = JSON.parse(JSON.stringify(window.STAGES));
    this.activeScreen = 'main-menu';
    this.activeTownPanel = 'character';
    this.generalLogs = [];
    this.activeSaveSlot = 1;
    this.dungeonMapState = { zoom: 1, panX: 0, panY: 0, selectedIndex: 0 };
    
    // Instantiate using window.CombatManager
    this.combatManager = new window.CombatManager(this);
    
    this.setupEquipmentPanel();
    this.initEventListeners();
    this.checkSavedGame();
    this.updateKeyboardHints();
  }

  addGeneralLog(message) {
    this.generalLogs.unshift(`[${new Date().toLocaleTimeString()}] ${message}`);
    if (this.generalLogs.length > 30) this.generalLogs.pop();
    this.updateGeneralLogsUI();
  }

  updateGeneralLogsUI() {
    const logBox = document.getElementById('town-event-logs');
    if (logBox) {
      logBox.innerHTML = this.generalLogs.map(l => `<div>${l}</div>`).join('');
    }
  }

  setupEquipmentPanel() {
    const target = document.getElementById('equipment-panel-content');
    if (!target || target.dataset.ready === 'true') return;

    const statsGrid = document.querySelector('#panel-character .stats-grid');
    const equipmentCard = statsGrid?.querySelector('.card:nth-child(2)');
    const inventorySection = document.querySelector('#panel-character .inventory-section');

    if (equipmentCard) target.appendChild(equipmentCard);
    if (inventorySection) target.appendChild(inventorySection);
    target.dataset.ready = 'true';
  }

  updateKeyboardHints() {
    const panel = document.getElementById('keyboard-hint-panel');
    if (!panel) return;

    const layout = this.getKeyboardLayout();
    panel.classList.toggle('is-combat', this.activeScreen === 'combat');
    panel.innerHTML = `
      <div class="keyboard-hint-title">
        <span>${layout.title}</span>
        <span class="keyboard-hint-subtitle">${layout.subtitle}</span>
      </div>
      ${layout.rows.map(row => `
        <div class="keyboard-row ${row.some(key => key.wide) ? 'has-space' : ''}">
          ${row.map(key => `
            <div class="keycap ${key.primary ? 'is-primary' : ''} ${key.disabled ? 'is-disabled' : ''}">
              <span class="keycap-key">${key.key}</span>
              <span class="keycap-label">${key.label}</span>
            </div>
          `).join('')}
        </div>
      `).join('')}
    `;
  }

  getKeyboardScope() {
    const activeModal = document.querySelector('.modal-overlay.active');
    if (activeModal) return activeModal;
    return document.getElementById(`screen-${this.activeScreen}`) || document;
  }

  getKeyboardTargets() {
    const scope = this.getKeyboardScope();
    const selectors = [
      'button:not(:disabled)',
      'input:not(:disabled)',
      '.class-card',
      '[data-keyboard-clickable="true"]'
    ];

    return Array.from(scope.querySelectorAll(selectors.join(','))).filter(element => {
      if (element.closest('#keyboard-hint-panel')) return false;
      if (element.disabled) return false;
      const rect = element.getBoundingClientRect();
      const style = window.getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none';
    });
  }

  focusKeyboardTarget(direction) {
    const targets = this.getKeyboardTargets();
    if (!targets.length) return false;

    if (typeof direction === 'number') {
      const currentIndex = targets.indexOf(document.activeElement);
      const nextIndex = currentIndex === -1
        ? (direction > 0 ? 0 : targets.length - 1)
        : (currentIndex + direction + targets.length) % targets.length;
      return this.focusKeyboardElement(targets[nextIndex]);
    }

    return this.focusKeyboardTargetByVector(direction.x, direction.y);
  }

  focusKeyboardElement(target) {
    if (!target) return false;
    target.focus({ preventScroll: true });
    this.scrollKeyboardTargetIntoView(target);
    return true;
  }

  getKeyboardScrollAnchor(target) {
    return target.closest([
      '.inventory-item',
      '.shop-item',
      '.stage-card',
      '.skill-upgrade-card',
      '.equip-slot',
      '.forge-card',
      '.save-slot-card',
      '.class-card',
      '.action-btn',
      '.town-tab-btn'
    ].join(',')) || target;
  }

  scrollKeyboardTargetIntoView(target) {
    const margin = 18;
    const anchor = this.getKeyboardScrollAnchor(target);
    let parent = anchor.parentElement;

    while (parent && parent !== document.body) {
      const style = window.getComputedStyle(parent);
      const canScrollY = /(auto|scroll)/.test(style.overflowY) && parent.scrollHeight > parent.clientHeight;
      const canScrollX = /(auto|scroll)/.test(style.overflowX) && parent.scrollWidth > parent.clientWidth;

      if (canScrollY || canScrollX) {
        const targetRect = anchor.getBoundingClientRect();
        const parentRect = parent.getBoundingClientRect();

        if (canScrollY && targetRect.height > parentRect.height - margin * 2) {
          parent.scrollBy({ top: targetRect.top - parentRect.top - margin, behavior: 'smooth' });
        } else if (canScrollY && targetRect.top < parentRect.top + margin) {
          parent.scrollBy({ top: targetRect.top - parentRect.top - margin, behavior: 'smooth' });
        } else if (canScrollY && targetRect.bottom > parentRect.bottom - margin) {
          parent.scrollBy({ top: targetRect.bottom - parentRect.bottom + margin, behavior: 'smooth' });
        }

        if (canScrollX && targetRect.left < parentRect.left + margin) {
          parent.scrollBy({ left: targetRect.left - parentRect.left - margin, behavior: 'smooth' });
        } else if (canScrollX && targetRect.right > parentRect.right - margin) {
          parent.scrollBy({ left: targetRect.right - parentRect.right + margin, behavior: 'smooth' });
        }
      }

      parent = parent.parentElement;
    }

    anchor.scrollIntoView({ block: 'nearest', inline: 'nearest', behavior: 'smooth' });
  }

  getSortedKeyboardTargets() {
    return [...this.getKeyboardTargets()].sort((a, b) => {
      const aRect = a.getBoundingClientRect();
      const bRect = b.getBoundingClientRect();
      return aRect.top - bRect.top || aRect.left - bRect.left;
    });
  }

  focusKeyboardTargetByVector(dx, dy) {
    const targets = this.getKeyboardTargets();
    if (!targets.length) return false;

    const sortedTargets = this.getSortedKeyboardTargets();

    const current = targets.includes(document.activeElement) ? document.activeElement : null;
    if (!current) return this.focusKeyboardElement(sortedTargets[0]);

    const currentRect = current.getBoundingClientRect();
    const currentCenter = {
      x: currentRect.left + currentRect.width / 2,
      y: currentRect.top + currentRect.height / 2
    };

    let best = null;
    let bestScore = Infinity;

    targets.forEach(target => {
      if (target === current) return;

      const rect = target.getBoundingClientRect();
      const center = {
        x: rect.left + rect.width / 2,
        y: rect.top + rect.height / 2
      };
      const deltaX = center.x - currentCenter.x;
      const deltaY = center.y - currentCenter.y;
      const forward = dx ? deltaX * dx : deltaY * dy;
      if (forward <= 6) return;

      const perpendicular = dx ? Math.abs(deltaY) : Math.abs(deltaX);
      const overlap = dx
        ? Math.max(0, Math.min(currentRect.bottom, rect.bottom) - Math.max(currentRect.top, rect.top))
        : Math.max(0, Math.min(currentRect.right, rect.right) - Math.max(currentRect.left, rect.left));
      const score = forward + perpendicular * 2.2 - overlap * 0.8;

      if (score < bestScore) {
        best = target;
        bestScore = score;
      }
    });

    if (best) return this.focusKeyboardElement(best);

    const currentIndex = sortedTargets.indexOf(current);
    const fallbackDirection = dx + dy > 0 ? 1 : -1;
    const fallbackIndex = (currentIndex + fallbackDirection + sortedTargets.length) % sortedTargets.length;
    return this.focusKeyboardElement(sortedTargets[fallbackIndex]);
  }

  activateKeyboardTarget() {
    const target = document.activeElement;
    if (!target || target === document.body || target.closest('#keyboard-hint-panel')) return false;
    if (target.matches('input, textarea')) return false;
    if (target.matches('.class-card, [data-keyboard-clickable="true"], button')) {
      const beforeScreen = this.activeScreen;
      const beforeModal = document.querySelector('.modal-overlay.active')?.id || '';
      const beforeTargets = this.getSortedKeyboardTargets();
      const beforeIndex = Math.max(0, beforeTargets.indexOf(target));
      target.click();
      setTimeout(() => {
        const afterModal = document.querySelector('.modal-overlay.active')?.id || '';
        if (this.activeScreen !== beforeScreen || afterModal !== beforeModal) return;
        if (document.activeElement?.matches('input, textarea')) return;

        const afterTargets = this.getSortedKeyboardTargets();
        if (!afterTargets.length) return;

        const nextIndex = Math.min(beforeIndex + 1, afterTargets.length - 1);
        this.focusKeyboardElement(afterTargets[nextIndex]);
      }, 80);
      return true;
    }
    return false;
  }

  getKeyboardLayout() {
    const activeModal = document.querySelector('.modal-overlay.active');
    if (activeModal?.id === 'modal-char-create') {
      return {
        title: '캐릭터 선택',
        subtitle: '방향키 + Space',
        rows: [
          [
            { key: '↑', label: '위' },
            { key: '←', label: '왼쪽' },
            { key: '↓', label: '아래' },
            { key: '→', label: '오른쪽' },
          ],
          [
            { key: 'Space', label: '선택', primary: true, wide: true },
          ],
        ],
      };
    }

    const titles = {
      'main-menu': '캐릭터 선택',
      town: '마을 메뉴',
      combat: '전투 조작',
    };
    const subtitles = {
      'main-menu': `저장 슬롯 ${this.activeSaveSlot}`,
      town: '방향키 + Space',
      combat: this.combatManager?.turn === 'player' ? '아군 턴' : '대기',
    };

    return {
      title: titles[this.activeScreen] || '조작',
      subtitle: subtitles[this.activeScreen] || '방향키 + Space',
      rows: [
        [
          { key: '↑', label: '위' },
          { key: '←', label: '왼쪽' },
          { key: '↓', label: '아래' },
          { key: '→', label: '오른쪽' },
        ],
        [
          { key: 'Space', label: this.activeScreen === 'main-menu' ? '불러오기 / 선택' : '선택 실행', primary: true, wide: true },
          { key: 'Esc', label: '뒤로' },
        ],
      ],
    };
  }

  handleKeyboardAction(event) {
    const key = event.key.toLowerCase();
    const activeTag = document.activeElement?.tagName;
    if ((activeTag === 'INPUT' || activeTag === 'TEXTAREA') && key !== 'arrowup' && key !== 'arrowdown' && key !== 'escape') return;

    const moveKeys = {
      arrowup: { x: 0, y: -1 },
      arrowleft: { x: -1, y: 0 },
      arrowdown: { x: 0, y: 1 },
      arrowright: { x: 1, y: 0 },
    };

    if (moveKeys[key]) {
      event.preventDefault();
      if (document.activeElement?.matches('input, textarea')) document.activeElement.blur();
      this.focusKeyboardTarget(moveKeys[key]);
      return;
    }

    if (key === 'escape') {
      event.preventDefault();
      if (document.activeElement?.matches('input, textarea')) document.activeElement.blur();

      const activeModal = document.querySelector('.modal-overlay.active');
      if (activeModal?.id === 'modal-char-create') {
        document.getElementById('btn-cancel-char')?.click();
      } else if (this.activeScreen === 'combat') {
        document.getElementById('btn-exit-dungeon')?.click();
      } else if (this.activeScreen === 'dungeon-map') {
        this.changeScreen('town');
        this.openTownPanel('character');
      } else if (this.activeScreen === 'town') {
        if (this.activeTownPanel !== 'character') this.openTownPanel('character');
        else this.changeScreen('main-menu');
      }
      return;
    }

    if (key !== ' ') return;
    event.preventDefault();

    if (this.activeScreen === 'main-menu') {
      const focusedSlot = document.activeElement?.classList?.contains('save-slot-card');
      const hasFocusedControl = document.activeElement && document.activeElement !== document.body;
      const loadButton = document.getElementById('btn-load-game');
      const newGameButton = document.getElementById('btn-new-game');

      if (focusedSlot || !hasFocusedControl) {
        if (loadButton && !loadButton.disabled) {
          loadButton.click();
        } else if (newGameButton && !newGameButton.disabled) {
          newGameButton.click();
        }
        return;
      }
    }

    if (this.activateKeyboardTarget()) return;

    const activeModal = document.querySelector('.modal-overlay.active');
    if (activeModal?.id === 'modal-char-create') {
      const selectedCard = activeModal.querySelector('.class-card.selected');
      if (selectedCard) selectedCard.focus();
      return;
    }

    const firstButton = this.getKeyboardTargets().find(target => target.matches('button:not(:disabled)'));
    if (firstButton) firstButton.click();
  }

  changeScreen(screenId) {
    this.screenChangeToken = (this.screenChangeToken || 0) + 1;
    const token = this.screenChangeToken;

    document.querySelectorAll('.screen').forEach(s => {
      s.classList.remove('active');
      setTimeout(() => {
        if (token === this.screenChangeToken) s.style.display = 'none';
      }, 300);
    });

    const targetScreen = document.getElementById(`screen-${screenId}`);
    if (targetScreen) {
      setTimeout(() => {
        if (token !== this.screenChangeToken) return;
        targetScreen.style.display = screenId === 'town' ? 'grid' : 'flex';
        setTimeout(() => {
          if (token === this.screenChangeToken) targetScreen.classList.add('active');
        }, 50);
      }, 300);
    }
    
    this.activeScreen = screenId;
    
    if (screenId === 'town') {
      this.updateTownHUD();
      this.refreshCharacterPanel();
    }

    this.updateKeyboardHints();
  }

  openTownPanel(panelId) {
    if (panelId === 'adventure') {
      this.openDungeonMapScreen();
      return;
    }

    this.activeTownPanel = panelId;
    document.querySelectorAll('.town-tab-btn').forEach(btn => {
      if (btn.dataset.panel === panelId) {
        btn.classList.add('active');
      } else {
        btn.classList.remove('active');
      }
    });

    document.querySelectorAll('.town-panel').forEach(p => {
      if (p.id === `panel-${panelId}`) {
        p.classList.add('active');
      } else {
        p.classList.remove('active');
      }
    });

    if (panelId === 'character') this.refreshCharacterPanel();
    else if (panelId === 'equipment') this.refreshEquipmentPanel();
    else if (panelId === 'skill') this.renderSkillUpgrades();
    else if (panelId === 'adventure') this.refreshAdventurePanel();
    else if (panelId === 'shop') this.refreshShopPanel();
    else if (panelId === 'forge') this.refreshForgePanel();

    this.updateKeyboardHints();
  }

  getSaveKey(slot = this.activeSaveSlot) {
    return `${SAVE_KEY_PREFIX}${slot}`;
  }

  getSaveData(slot = this.activeSaveSlot) {
    const saveStr = localStorage.getItem(this.getSaveKey(slot));
    if (!saveStr) return null;
    try {
      return JSON.parse(saveStr);
    } catch (e) {
      console.error(e);
      return null;
    }
  }

  migrateLegacySaveIfNeeded() {
    const legacySave = localStorage.getItem(LEGACY_SAVE_KEY);
    if (!legacySave || localStorage.getItem(this.getSaveKey(1))) return;
    localStorage.setItem(this.getSaveKey(1), legacySave);
    localStorage.removeItem(LEGACY_SAVE_KEY);
  }

  getSaveSlotSummary(slot) {
    const save = this.getSaveData(slot);
    if (!save?.player) return { empty: true };
    const className = window.CLASS_DETAILS?.[save.player.classType]?.name || save.player.classType || '직업';
    return {
      empty: false,
      name: save.player.name || '이름 없음',
      className,
      level: save.player.level || 1,
      updatedAt: save.updatedAt || null
    };
  }

  renderSaveSlots() {
    const container = document.getElementById('save-slots-grid');
    if (!container) return;

    container.innerHTML = '';
    for (let slot = 1; slot <= SAVE_SLOT_COUNT; slot++) {
      const summary = this.getSaveSlotSummary(slot);
      const button = document.createElement('button');
      button.className = `save-slot-card ${slot === this.activeSaveSlot ? 'selected' : ''}`;
      button.innerHTML = summary.empty
        ? `
          <div class="save-slot-number">슬롯 ${slot}</div>
          <div class="save-slot-name">비어 있음</div>
          <div class="save-slot-meta">새 모험 가능</div>
        `
        : `
          <div class="save-slot-number">슬롯 ${slot}</div>
          <div class="save-slot-name">${summary.name}</div>
          <div class="save-slot-meta">Lv.${summary.level} ${summary.className}<br>${summary.updatedAt ? new Date(summary.updatedAt).toLocaleString() : '저장 시간 없음'}</div>
        `;
      button.onclick = () => {
        this.activeSaveSlot = slot;
        this.checkSavedGame();
        this.updateKeyboardHints();
      };
      button.onfocus = () => {
        this.activeSaveSlot = slot;
        document.querySelectorAll('.save-slot-card').forEach((card, index) => {
          card.classList.toggle('selected', index + 1 === slot);
        });
        const loadBtn = document.getElementById('btn-load-game');
        if (loadBtn) loadBtn.disabled = !localStorage.getItem(this.getSaveKey(slot));
        this.updateKeyboardHints();
      };
      container.appendChild(button);
    }
  }

  saveGame({ manual = false } = {}) {
    if (!this.player) return false;
    if (this.activeScreen !== 'town') {
      if (manual) alert('저장은 마을에서만 가능합니다.');
      return false;
    }

    this.addGeneralLog(manual ? `💾 슬롯 ${this.activeSaveSlot}에 수동 저장했습니다.` : `💾 슬롯 ${this.activeSaveSlot}에 자동 저장되었습니다.`);
    
    const saveData = {
      version: 2,
      slot: this.activeSaveSlot,
      updatedAt: new Date().toISOString(),
      player: {
        name: this.player.name,
        classType: this.player.classType,
        level: this.player.level,
        exp: this.player.exp,
        maxExp: this.player.maxExp,
        hp: this.player.hp,
        mp: this.player.mp,
        gold: this.player.gold,
        equipped: this.player.equipped,
        skillLevels: this.player.skillLevels
      },
      inventory: this.inventory,
      stages: this.stages.map(s => ({ id: s.id, bossCleared: s.bossCleared })),
      generalLogs: this.generalLogs
    };

    localStorage.setItem(this.getSaveKey(), JSON.stringify(saveData));
    this.checkSavedGame();
    return true;
  }

  checkSavedGame() {
    this.migrateLegacySaveIfNeeded();
    this.renderSaveSlots();
    const save = localStorage.getItem(this.getSaveKey());
    const loadBtn = document.getElementById('btn-load-game');
    if (save) {
      loadBtn.disabled = false;
    } else {
      loadBtn.disabled = true;
    }
  }

  loadGame(slot = this.activeSaveSlot) {
    const saveStr = localStorage.getItem(this.getSaveKey(slot));
    if (!saveStr) return;
    
    try {
      const save = JSON.parse(saveStr);
      this.activeSaveSlot = slot;
      
      this.player = new window.PlayerCharacter(save.player.name, save.player.classType);
      this.player.level = save.player.level;
      this.player.exp = save.player.exp;
      this.player.maxExp = save.player.maxExp;
      this.player.hp = save.player.hp;
      this.player.mp = save.player.mp;
      this.player.gold = save.player.gold;
      this.player.equipped = window.normalizeEquipped(save.player.equipped);
      this.player.skillLevels = save.player.skillLevels || this.player.skillLevels;
      this.player.syncSkills();
      this.player.recalculateStats();

      this.inventory = (save.inventory || []).filter(item => item.type !== window.ITEM_TYPES.POTION);
      
      if (save.stages) {
        save.stages.forEach(savedStage => {
          const match = this.stages.find(s => s.id === savedStage.id);
          if (match) match.bossCleared = savedStage.bossCleared;
        });
      }
      
      this.generalLogs = save.generalLogs || [];
      this.addGeneralLog(`📂 슬롯 ${this.activeSaveSlot}의 모험 기록을 성공적으로 불러왔습니다.`);
      
      this.changeScreen('town');
      this.openTownPanel('character');
    } catch (e) {
      console.error(e);
      alert('저장 데이터 파일을 불러오는데 실패했습니다.');
    }
  }

  startNewGame(name, classType) {
    this.player = new window.PlayerCharacter(name, classType);
    this.inventory = [];
    this.stages = JSON.parse(JSON.stringify(window.STAGES));
    this.generalLogs = [];
    
    this.player.recalculateStats();
    this.player.hp = this.player.maxHp;
    this.player.mp = this.player.maxMp;

    this.addGeneralLog(`⚔️ 새로운 차원의 용사 **${name}**(${window.CLASS_DETAILS[classType].name})가 탄생했습니다!`);
    
    this.changeScreen('town');
    this.openTownPanel('character');
    this.saveGame();
    this.checkSavedGame();
  }

  updateTownHUD() {
    if (!this.player) return;
    this.player.recalculateStats();
    
    document.getElementById('hud-player-level').textContent = `Lv.${this.player.level}`;
    document.getElementById('hud-player-name').textContent = this.player.name;
    document.getElementById('hud-player-class').textContent = window.CLASS_DETAILS[this.player.classType].name;
    const sketchCaption = document.getElementById('sketch-character-caption');
    if (sketchCaption) {
      sketchCaption.textContent = `${this.player.name} · ${window.CLASS_DETAILS[this.player.classType].name}`;
    }
    
    document.getElementById('hud-hp-bar').style.width = `${(this.player.hp / this.player.maxHp) * 100}%`;
    document.getElementById('hud-hp-text').textContent = `${this.player.hp} / ${this.player.maxHp}`;
    
    document.getElementById('hud-mp-bar').style.width = `${(this.player.mp / this.player.maxMp) * 100}%`;
    document.getElementById('hud-mp-text').textContent = `${this.player.mp} / ${this.player.maxMp}`;

    document.getElementById('hud-xp-bar').style.width = `${(this.player.exp / this.player.maxExp) * 100}%`;
    document.getElementById('hud-xp-text').textContent = `${this.player.exp} / ${this.player.maxExp}`;

    document.getElementById('hud-gold').textContent = `${this.player.gold} G`;
  }

  refreshCharacterPanel() {
    if (!this.player) return;
    this.player.recalculateStats();
    this.updateTownHUD();
    
    document.getElementById('stat-level').textContent = this.player.level;
    document.getElementById('stat-hp').textContent = `${this.player.hp} / ${this.player.maxHp}`;
    document.getElementById('stat-mp').textContent = `${this.player.mp} / ${this.player.maxMp}`;
    document.getElementById('stat-atk-phys').textContent = this.player.physicalAttack;
    document.getElementById('stat-atk-magic').textContent = this.player.magicAttack;
    document.getElementById('stat-def').textContent = this.player.getEffectiveDefense();
    document.getElementById('stat-spd').textContent = this.player.speed;
    document.getElementById('stat-crit').textContent = `${Math.round(this.player.critRate * 100)}%`;
    document.getElementById('stat-crit-dmg').textContent = `${Math.round((1 + this.player.critDamage) * 100)}%`;
    document.getElementById('stat-evasion').textContent = `${Math.round(this.player.getEffectiveEvasion() * 100)}%`;
    document.getElementById('stat-damage-reduction').textContent = `${Math.round(this.player.damageReduction * 100)}%`;
    document.getElementById('stat-life-steal').textContent = `${Math.round(this.player.lifeSteal * 100)}%`;
    const totalManaRegenPercent = (window.BASE_TURN_MANA_REGEN + this.player.manaRegen) * 100;
    document.getElementById('stat-mana-regen').textContent = `${Number.isInteger(totalManaRegenPercent) ? totalManaRegenPercent : totalManaRegenPercent.toFixed(1)}%`;
    document.getElementById('stat-status-power').textContent = `+${Math.round(this.player.statusPower * 100)}%`;
    document.getElementById('stat-item-find').textContent = `+${Math.round(this.player.itemFind * 100)}%`;

    this.renderEquipSlot('weapon', '🗡️');
    this.renderEquipSlot('armor', '🛡️');
    this.renderEquipSlot('accessory', '📿', '액세서리 1');
    this.renderEquipSlot('accessory2', '📿', '액세서리 2');
    this.renderEquipSlot('accessory3', '📿', '액세서리 3');
    this.renderEquipSlot('accessory4', '📿', '액세서리 4');

    this.renderSkillUpgrades();
    this.renderInventory();
  }

  refreshEquipmentPanel() {
    if (!this.player) return;
    this.player.recalculateStats();
    this.updateTownHUD();

    this.renderEquipSlot('weapon', '🗡️');
    this.renderEquipSlot('armor', '🛡️');
    this.renderEquipSlot('accessory', '📿', '액세서리 1');
    this.renderEquipSlot('accessory2', '📿', '액세서리 2');
    this.renderEquipSlot('accessory3', '📿', '액세서리 3');
    this.renderEquipSlot('accessory4', '📿', '액세서리 4');
    this.renderInventory();
  }

  getGearTypeLabel(type) {
    if (type === window.ITEM_TYPES.WEAPON) return '무기';
    if (type === window.ITEM_TYPES.ARMOR) return '방어구';
    if (type === window.ITEM_TYPES.ACCESSORY || type?.startsWith('accessory')) return '액세서리';
    return '장비';
  }

  getGearTypeIcon(type) {
    if (type === window.ITEM_TYPES.WEAPON) return '🗡️';
    if (type === window.ITEM_TYPES.ARMOR) return '🛡️';
    if (type === window.ITEM_TYPES.ACCESSORY || type?.startsWith('accessory')) return '📿';
    return '🎁';
  }

  renderEquipSlot(slotType, defaultEmoji, labelOverride = null) {
    const slot = document.getElementById(`slot-${slotType}`);
    if (!slot) return;
    const item = this.player.equipped[slotType];
    const slotLabel = labelOverride || this.getGearTypeLabel(slotType);
    const slotIcon = this.getGearTypeIcon(slotType);
    
    if (item) {
      slot.classList.add('equipped');
      slot.innerHTML = `
        <div class="item-icon-placeholder ${item.rarity.class}">${slotIcon}</div>
        <div class="equip-slot-details">
          <div class="equip-slot-title">${slotLabel}</div>
          <div class="equip-item-name" style="color: ${item.rarity.color}">
            ${item.name} ${item.upgradeLevel > 0 ? `+${item.upgradeLevel}` : ''}
          </div>
          <div style="font-size: 0.75rem; color: var(--text-muted)">${item.description}</div>
        </div>
        <button class="btn btn-equip" style="padding: 4px 8px; font-size: 0.75rem">해제</button>
      `;
      slot.querySelector('.btn-equip').onclick = () => this.unequipItem(slotType);
    } else {
      slot.classList.remove('equipped');
      slot.innerHTML = `
        <div class="item-icon-placeholder">${defaultEmoji}</div>
        <div class="equip-slot-details">
          <div class="equip-slot-title">${slotLabel}</div>
          <div class="equip-item-name" style="color: var(--text-muted)">장착 슬롯 비어있음</div>
        </div>
      `;
    }
  }

  renderInventory() {
    const container = document.getElementById('inventory-items-grid');
    container.innerHTML = '';

    if (this.inventory.length === 0) {
      container.innerHTML = `<div style="grid-column: 1/-1; text-align: center; color: var(--text-muted); padding: 20px;">가방이 텅 비어 있습니다.</div>`;
      return;
    }

    this.inventory.forEach(item => {
      const card = document.createElement('div');
      card.className = 'inventory-item';
      
      let badgeHtml = '';
      let actionBtnHtml = '';
      
      if (item.type === window.ITEM_TYPES.ACCESSORY) {
        badgeHtml = `<span style="font-size: 0.75rem; color: ${item.rarity.color}; font-weight: bold;">${item.rarity.name} ${this.getGearTypeLabel(item.type)}</span>`;
        actionBtnHtml = window.ACCESSORY_EQUIP_SLOTS
          .map((slotName, idx) => `<button class="btn btn-primary btn-equip" data-slot="${slotName}">${idx + 1}번</button>`)
          .join('');
      } else {
        badgeHtml = `<span style="font-size: 0.75rem; color: ${item.rarity.color}; font-weight: bold;">${item.rarity.name} ${this.getGearTypeLabel(item.type)}</span>`;
        actionBtnHtml = `<button class="btn btn-primary btn-equip">장착</button>`;
      }
      
      const sellVal = window.getGearSellValue(item);

      card.innerHTML = `
        <div class="item-main-row">
          <div class="item-icon-placeholder ${item.rarity ? item.rarity.class : ''}">
            ${this.getGearTypeIcon(item.type)}
          </div>
          <div class="item-info">
            <div class="item-name" style="color: ${item.rarity ? item.rarity.color : 'inherit'}">
              ${item.name} ${item.upgradeLevel > 0 ? `+${item.upgradeLevel}` : ''}
            </div>
            <div class="item-desc">${item.description}</div>
            <div style="font-size: 0.7rem; color: var(--text-muted); margin-top: 2px;">
              ${badgeHtml} • 판매가 ${sellVal}G
            </div>
          </div>
        </div>
        <div class="item-actions">
          ${actionBtnHtml}
          <button class="btn btn-sell">판매</button>
        </div>
      `;

      const equipBtns = card.querySelectorAll('.btn-equip');
      const sellBtn = card.querySelector('.btn-sell');

      if (equipBtns.length > 0) {
        equipBtns.forEach(btn => {
          btn.onclick = () => this.equipItem(item, btn.dataset.slot || null);
        });
      }
      sellBtn.onclick = () => this.sellItem(item, sellVal);

      container.appendChild(card);
    });
  }

  getSkillStatSummary(skill) {
    const parts = [];
    if (typeof skill.power === 'number') parts.push(`위력 ${Math.round(skill.power * 100)}%`);
    const chanceLabels = {
      stunChance: '기절',
      burnChance: '화상',
      poisonChance: '중독',
      bleedChance: '출혈',
      shockChance: '감전',
      freezeChance: '빙결',
      blindChance: '실명',
      weakenChance: '약화',
      vulnerableChance: '취약',
      silenceChance: '침묵',
      manaBurnChance: '마나 연소',
      regenChance: '재생'
    };
    Object.entries(chanceLabels).forEach(([field, label]) => {
      if (typeof skill[field] === 'number') parts.push(`${label} ${Math.round(skill[field] * 100)}%`);
    });
    if (skill.lifeStealRatio) parts.push(`기술 흡혈 ${Math.round(skill.lifeStealRatio * 100)}%`);
    if (skill.selfStatus) {
      const definition = window.STATUS_EFFECT_DEFINITIONS?.[skill.selfStatus.type];
      parts.push(`${definition?.name || skill.selfStatus.type} ${skill.selfStatus.duration || 2}턴`);
    }
    if (skill.forceCrit) parts.push('치명타 확정');
    return parts.join(' · ');
  }

  getSkillUpgradePreview(skill, nextSkill) {
    const parts = [`마나 ${skill.cost} ➔ ${nextSkill.cost}`];

    if (typeof skill.power === 'number') {
      parts.push(`위력 ${Math.round(skill.power * 100)}% ➔ ${Math.round(nextSkill.power * 100)}%`);
    }
    const chanceLabels = {
      stunChance: '기절',
      burnChance: '화상',
      poisonChance: '중독',
      bleedChance: '출혈',
      shockChance: '감전',
      freezeChance: '빙결',
      blindChance: '실명',
      weakenChance: '약화',
      vulnerableChance: '취약',
      silenceChance: '침묵',
      manaBurnChance: '마나 연소',
      regenChance: '재생'
    };
    Object.entries(chanceLabels).forEach(([field, label]) => {
      if (typeof skill[field] === 'number') {
        parts.push(`${label} ${Math.round(skill[field] * 100)}% ➔ ${Math.round(nextSkill[field] * 100)}%`);
      }
    });
    if (skill.forceCrit) {
      parts.push('치명타 확정 유지');
    }

    return parts.join(' / ');
  }

  renderSkillUpgrades() {
    const container = document.getElementById('skill-upgrade-list');
    if (!container) return;

    container.innerHTML = '';

    this.player.skills.forEach(skill => {
      const nextSkill = window.getScaledSkill(
        window.CLASS_DETAILS[this.player.classType].skills.find(baseSkill => baseSkill.id === skill.id),
        skill.level + 1
      );
      const cost = this.player.getSkillUpgradeCost(skill.id);
      const canAfford = this.player.gold >= cost;
      const card = document.createElement('div');
      card.className = 'skill-upgrade-card';

      card.innerHTML = `
        <div class="skill-upgrade-main">
          <div>
            <div class="skill-upgrade-name">${skill.name} <span>Lv.${skill.level}</span></div>
            <div class="skill-upgrade-desc">${this.getSkillStatSummary(skill)}</div>
            <div class="skill-upgrade-next">다음: ${this.getSkillUpgradePreview(skill, nextSkill)}</div>
          </div>
          <button class="btn btn-primary" ${canAfford ? '' : 'disabled'}>강화 ${cost}G</button>
        </div>
      `;

      card.querySelector('button').onclick = () => this.upgradeSkill(skill.id, cost);
      container.appendChild(card);
    });
  }

  upgradeSkill(skillId, cost) {
    if (this.player.gold < cost) {
      alert('골드가 부족합니다!');
      return;
    }

    const before = this.player.skills.find(skill => skill.id === skillId);
    this.player.gold -= cost;
    const upgraded = this.player.upgradeSkill(skillId);

    this.addGeneralLog(`📘 [${before.name}] 스킬이 Lv.${upgraded.level}로 강화되었습니다. 마나 소모가 늘고 위력/확률이 함께 증가했습니다. (-${cost}골드)`);
    this.updateTownHUD();
    if (this.activeTownPanel === 'skill') {
      this.renderSkillUpgrades();
    } else {
      this.refreshCharacterPanel();
    }
    this.saveGame();
  }

  getEquipSlotForItem(item, preferredSlot = null) {
    if (item.type === window.ITEM_TYPES.ACCESSORY) {
      if (preferredSlot && window.ACCESSORY_EQUIP_SLOTS.includes(preferredSlot)) {
        return preferredSlot;
      }

      return window.ACCESSORY_EQUIP_SLOTS.find(slotName => !this.player.equipped[slotName])
        || window.ACCESSORY_EQUIP_SLOTS[0];
    }

    return item.type;
  }

  equipItem(item, preferredSlot = null) {
    this.player.equipped = window.normalizeEquipped(this.player.equipped);
    const slot = this.getEquipSlotForItem(item, preferredSlot);
    const currentEquipped = this.player.equipped[slot];
    
    const index = this.inventory.findIndex(i => i.id === item.id);
    if (index !== -1) {
      this.inventory.splice(index, 1);
    }

    if (currentEquipped) {
      this.inventory.push(currentEquipped);
    }

    this.player.equipped[slot] = item;
    this.player.recalculateStats();
    
    this.addGeneralLog(`✨ [${item.name}]을(를) 장착했습니다.`);
    this.refreshCharacterPanel();
    this.saveGame();
  }

  unequipItem(slotType) {
    const item = this.player.equipped[slotType];
    if (!item) return;

    this.player.equipped[slotType] = null;
    this.inventory.push(item);
    this.player.recalculateStats();

    this.addGeneralLog(`📿 [${item.name}] 해제하여 가방에 보관했습니다.`);
    this.refreshCharacterPanel();
    this.saveGame();
  }

  sellItem(item, price) {
    const index = this.inventory.findIndex(i => i.id === item.id);
    if (index !== -1) {
      this.inventory.splice(index, 1);
    }
    this.player.gainGold(price);
    this.addGeneralLog(`💰 [${item.name}]을(를) 매각하여 **${price}**골드를 벌었습니다.`);
    this.refreshCharacterPanel();
    this.saveGame();
  }

  escapeHtml(value) {
    const replacements = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
    return String(value ?? '').replace(/[&<>"']/g, char => replacements[char]);
  }

  getDungeonMapRegions() {
    return [
      { key: 'frost', name: '서리 대지', center: [22, 17], points: [[17, 17], [23, 12], [30, 20], [20, 28], [33, 26]] },
      { key: 'green', name: '그린위드', center: [49, 19], points: [[43, 18], [48, 12], [55, 17], [46, 27], [57, 29]] },
      { key: 'lava', name: '용암 고원', center: [79, 20], points: [[70, 18], [78, 12], [86, 23], [73, 29], [90, 29]] },
      { key: 'elesia', name: '엘레시아', center: [18, 47], points: [[9, 46], [16, 39], [24, 47], [19, 56], [30, 54]] },
      { key: 'arcadia', name: '아르카디아', center: [49, 50], points: [[41, 47], [49, 38], [56, 49], [45, 58], [58, 60]] },
      { key: 'desert', name: '황금 사막', center: [77, 50], points: [[66, 46], [75, 40], [86, 48], [72, 57], [90, 58]] },
      { key: 'shadow', name: '그림자 땅', center: [19, 76], points: [[11, 74], [17, 66], [26, 74], [20, 85], [31, 83]] },
      { key: 'isles', name: '푸른 군도', center: [49, 80], points: [[38, 73], [47, 69], [55, 76], [44, 87], [57, 88]] },
      { key: 'wind', name: '바람의 땅', center: [78, 78], points: [[67, 75], [76, 68], [88, 73], [73, 86], [90, 87]] }
    ];
  }

  isDungeonLocked(index) {
    return index > 0 && !this.stages[index - 1]?.bossCleared;
  }

  getDungeonMapEntries() {
    const regions = this.getDungeonMapRegions();
    const regionsByKey = Object.fromEntries(regions.map(region => [region.key, region]));
    const regionByStageId = {
      whispering_woods: 'green',
      forgotten_crypt: 'arcadia',
      void_citadel: 'shadow',
      crimson_mine: 'lava',
      storm_spire: 'wind',
      frozen_palace: 'frost',
      ashen_wasteland: 'lava',
      moonlit_labyrinth: 'elesia',
      sunken_archive: 'isles',
      eternal_rift: 'shadow',
      obsidian_sanctum: 'lava',
      starfall_bastion: 'frost',
      dream_mire: 'green',
      ironwood_grove: 'green',
      aurora_prison: 'frost',
      abyssal_observatory: 'shadow',
      bloodmoon_cathedral: 'shadow',
      crystal_timeway: 'arcadia',
      voidforge_nexus: 'lava',
      origin_throne: 'arcadia',
      radiant_oratory: 'elesia',
      thunder_hollow: 'isles',
      plague_garden: 'green',
      mirrored_bastion: 'arcadia',
      ashstorm_front: 'wind',
      frozen_starfall: 'frost',
      serpent_scriptorium: 'desert',
      eclipse_spire: 'wind',
      primal_maelstrom: 'isles',
      aetheria_core: 'arcadia',
      silver_moon_archive: 'elesia',
      ashen_clocktower: 'wind',
      crystal_abyss: 'isles',
      silent_sun_temple: 'desert',
      eclipse_colosseum: 'wind',
      seraphic_ruins: 'elesia',
      thousand_venom_vault: 'desert',
      stormglass_sea: 'isles',
      red_mirror_maze: 'desert',
      infinite_constellation: 'frost',
      black_lotus_sanctum: 'elesia',
      primordial_bellows: 'lava',
      void_orchestra: 'shadow',
      crownless_throne: 'desert',
      root_of_origin: 'green'
    };
    const regionStageCounts = {};

    return this.stages.map((stage, index) => {
      const regionKey = regionByStageId[stage.id] || regions[Math.min(regions.length - 1, Math.floor(index / 5))].key;
      const region = regionsByKey[regionKey] || regions[0];
      const regionalIndex = regionStageCounts[region.key] || 0;
      regionStageCounts[region.key] = regionalIndex + 1;
      const point = region.points[regionalIndex % region.points.length];
      return {
        stage,
        index,
        number: index + 1,
        region,
        x: point[0],
        y: point[1],
        locked: this.isDungeonLocked(index),
        cleared: Boolean(stage.bossCleared)
      };
    });
  }

  openDungeonMapScreen() {
    this.activeTownPanel = 'adventure';
    document.querySelectorAll('.town-tab-btn').forEach(btn => {
      btn.classList.toggle('active', btn.dataset.panel === 'adventure');
    });

    const entries = this.getDungeonMapEntries();
    const target = entries.find(entry => !entry.locked && !entry.cleared) || entries.find(entry => !entry.locked) || entries[0];
    if (target) this.dungeonMapState.selectedIndex = target.index;
    this.dungeonMapState.zoom = 1;
    this.dungeonMapState.panX = 0;
    this.dungeonMapState.panY = 0;

    this.renderDungeonMapScreen();
    this.changeScreen('dungeon-map');
    this.updateKeyboardHints();
  }

  renderDungeonMapScreen() {
    const regionsContainer = document.getElementById('dungeon-map-regions');
    const pinsContainer = document.getElementById('dungeon-map-pins');
    if (!regionsContainer || !pinsContainer) return;

    const regions = this.getDungeonMapRegions();
    const entries = this.getDungeonMapEntries();

    regionsContainer.innerHTML = regions.map(region => `
      <button type="button" class="dungeon-map-region-hit" data-map-region="${region.key}"
        style="left:${region.center[0]}%; top:${region.center[1]}%;"
        aria-label="${this.escapeHtml(region.name)}"></button>
      <button type="button" class="dungeon-region-label" data-map-region="${region.key}" style="left:${region.center[0]}%; top:${region.center[1]}%;">
        ${this.escapeHtml(region.name)}
      </button>
    `).join('');

    pinsContainer.innerHTML = entries.map(entry => `
      <button type="button" class="dungeon-map-pin ${entry.locked ? 'locked' : ''} ${entry.cleared ? 'cleared' : ''} ${entry.index === this.dungeonMapState.selectedIndex ? 'selected' : ''}"
        data-map-stage="${entry.index}" style="left:${entry.x}%; top:${entry.y}%;" aria-label="${this.escapeHtml(entry.stage.name)}">
        <span>${entry.number}</span>
        <span class="pin-label">${entry.number}. ${this.escapeHtml(entry.stage.name)}</span>
      </button>
    `).join('');

    this.bindDungeonMapEvents();
    this.applyDungeonMapTransform();
    this.selectDungeonMapStage(this.dungeonMapState.selectedIndex, false);
  }

  bindDungeonMapEvents() {
    document.querySelectorAll('[data-map-region]').forEach(button => {
      button.onclick = event => {
        event.stopPropagation();
        this.selectDungeonMapRegion(button.dataset.mapRegion);
      };
    });

    document.querySelectorAll('[data-map-stage]').forEach(button => {
      button.onclick = event => {
        event.stopPropagation();
        this.selectDungeonMapStage(Number(button.dataset.mapStage), false);
      };
    });

    const backButton = document.getElementById('btn-dungeon-map-back');
    if (backButton) {
      backButton.onclick = () => {
        this.changeScreen('town');
        this.openTownPanel('character');
      };
    }

    if (this.dungeonMapControlsReady) {
      this.applyDungeonMapTransform();
      return;
    }

    this.dungeonMapControlsReady = true;
    document.getElementById('btn-map-zoom-in')?.addEventListener('click', () => this.setDungeonMapZoom(this.dungeonMapState.zoom + 0.35));
    document.getElementById('btn-map-zoom-out')?.addEventListener('click', () => this.setDungeonMapZoom(this.dungeonMapState.zoom - 0.35));
    document.getElementById('btn-map-reset')?.addEventListener('click', () => {
      this.dungeonMapState.zoom = 1;
      this.dungeonMapState.panX = 0;
      this.dungeonMapState.panY = 0;
      this.applyDungeonMapTransform();
    });

    const viewport = document.getElementById('dungeon-map-viewport');
    if (!viewport) return;

    viewport.addEventListener('wheel', event => {
      event.preventDefault();
      this.setDungeonMapZoom(this.dungeonMapState.zoom + (event.deltaY > 0 ? -0.22 : 0.22), event);
    }, { passive: false });

    let dragStart = null;
    viewport.addEventListener('pointerdown', event => {
      if (event.target.closest('button')) return;
      dragStart = {
        x: event.clientX,
        y: event.clientY,
        panX: this.dungeonMapState.panX,
        panY: this.dungeonMapState.panY
      };
      viewport.classList.add('dragging');
      viewport.setPointerCapture(event.pointerId);
    });

    viewport.addEventListener('pointermove', event => {
      if (!dragStart) return;
      this.dungeonMapState.panX = dragStart.panX + event.clientX - dragStart.x;
      this.dungeonMapState.panY = dragStart.panY + event.clientY - dragStart.y;
      this.clampDungeonMapPan();
      this.applyDungeonMapTransform();
    });

    const stopDrag = event => {
      dragStart = null;
      viewport.classList.remove('dragging');
      if (event?.pointerId !== undefined && viewport.hasPointerCapture(event.pointerId)) {
        viewport.releasePointerCapture(event.pointerId);
      }
    };

    viewport.addEventListener('pointerup', stopDrag);
    viewport.addEventListener('pointercancel', stopDrag);
  }

  selectDungeonMapRegion(regionKey) {
    const regions = this.getDungeonMapRegions();
    const region = regions.find(item => item.key === regionKey);
    if (!region) return;

    const entries = this.getDungeonMapEntries().filter(entry => entry.region.key === regionKey);
    const target = entries.find(entry => !entry.locked && !entry.cleared) || entries.find(entry => !entry.locked) || entries[0];
    this.focusDungeonMapPoint(region.center[0], region.center[1], 2.25);
    if (target) this.selectDungeonMapStage(target.index, false);
  }

  isDungeonMapZoomed() {
    return this.dungeonMapState.zoom > 1.24;
  }

  renderDungeonMapIntro() {
    const detail = document.getElementById('dungeon-map-detail');
    if (!detail) return;

    detail.innerHTML = `
      <div class="dungeon-map-status">지도 탐색</div>
      <h3>대륙을 확대해 던전을 찾으세요</h3>
      <p>처음 지도에 들어오면 던전 위치는 숨겨집니다. 마우스 휠이나 + 버튼으로 지도를 확대하면 현재 보이는 지역의 던전 표시와 진입 정보가 나타납니다.</p>
      <div class="dungeon-map-stat-grid">
        <div class="dungeon-map-stat"><span>표시 조건</span><strong>125% 이상</strong></div>
        <div class="dungeon-map-stat"><span>지도 축소</span><strong>100%</strong></div>
      </div>
    `;
  }

  selectDungeonMapStage(index, focus = false) {
    const entries = this.getDungeonMapEntries();
    const entry = entries[index] || entries.find(item => !item.locked) || entries[0];
    if (!entry) return;

    this.dungeonMapState.selectedIndex = entry.index;
    if (focus) this.focusDungeonMapPoint(entry.x, entry.y, Math.max(this.dungeonMapState.zoom, 2.05));

    document.querySelectorAll('[data-map-stage]').forEach(button => {
      button.classList.toggle('selected', Number(button.dataset.mapStage) === entry.index);
    });
    document.querySelectorAll('[data-map-region]').forEach(button => {
      button.classList.toggle('active', button.dataset.mapRegion === entry.region.key);
    });

    const detail = document.getElementById('dungeon-map-detail');
    if (!detail) return;

    if (!this.isDungeonMapZoomed()) {
      this.renderDungeonMapIntro();
      return;
    }

    const statusClass = entry.locked ? 'locked' : entry.cleared ? 'cleared' : '';
    const statusText = entry.locked ? '잠금: 이전 던전 보스 클리어 필요' : entry.cleared ? '토벌 완료' : '진입 가능';
    detail.innerHTML = `
      <div class="dungeon-map-status ${statusClass}">${statusText}</div>
      <h3>${entry.number}. ${this.escapeHtml(entry.stage.name)}</h3>
      <p>${this.escapeHtml(entry.stage.description)}</p>
      <div class="dungeon-map-stat-grid">
        <div class="dungeon-map-stat"><span>대륙</span><strong>${this.escapeHtml(entry.region.name)}</strong></div>
        <div class="dungeon-map-stat"><span>권장 레벨</span><strong>Lv.${entry.stage.recommendedLevel}</strong></div>
        <div class="dungeon-map-stat"><span>구역 수</span><strong>${entry.stage.floors}</strong></div>
        <div class="dungeon-map-stat"><span>진행</span><strong>${entry.cleared ? '완료' : entry.locked ? '잠금' : '대기'}</strong></div>
      </div>
      <button id="btn-dungeon-map-enter" class="btn btn-primary dungeon-map-enter" ${entry.locked ? 'disabled' : ''}>던전 진입</button>
    `;

    const enterButton = document.getElementById('btn-dungeon-map-enter');
    if (enterButton) {
      enterButton.onclick = () => {
        if (!entry.locked) this.enterDungeon(entry.stage);
      };
    }
  }

  focusDungeonMapPoint(x, y, zoom = 2.2) {
    const plane = document.getElementById('dungeon-map-plane');
    if (!plane) return;
    const width = plane.clientWidth || 960;
    const height = plane.clientHeight || 720;
    this.dungeonMapState.zoom = Math.max(1, Math.min(4.5, zoom));
    this.dungeonMapState.panX = -((x / 100 - 0.5) * width * this.dungeonMapState.zoom);
    this.dungeonMapState.panY = -((y / 100 - 0.5) * height * this.dungeonMapState.zoom);
    this.clampDungeonMapPan();
    this.applyDungeonMapTransform();
  }

  setDungeonMapZoom(nextZoom, origin = null) {
    const viewport = document.getElementById('dungeon-map-viewport');
    const oldZoom = this.dungeonMapState.zoom;
    const newZoom = Math.max(1, Math.min(4.5, nextZoom));

    if (viewport) {
      const rect = viewport.getBoundingClientRect();
      const originX = origin?.clientX ?? rect.left + rect.width / 2;
      const originY = origin?.clientY ?? rect.top + rect.height / 2;
      const beforeX = (originX - rect.left - rect.width / 2 - this.dungeonMapState.panX) / oldZoom;
      const beforeY = (originY - rect.top - rect.height / 2 - this.dungeonMapState.panY) / oldZoom;
      this.dungeonMapState.panX = originX - rect.left - rect.width / 2 - beforeX * newZoom;
      this.dungeonMapState.panY = originY - rect.top - rect.height / 2 - beforeY * newZoom;
    }

    this.dungeonMapState.zoom = newZoom;
    this.clampDungeonMapPan();
    this.applyDungeonMapTransform();
  }

  clampDungeonMapPan() {
    const viewport = document.getElementById('dungeon-map-viewport');
    const plane = document.getElementById('dungeon-map-plane');
    if (!viewport || !plane) return;

    if (this.dungeonMapState.zoom <= 1.001) {
      this.dungeonMapState.panX = 0;
      this.dungeonMapState.panY = 0;
      return;
    }

    const viewportRect = viewport.getBoundingClientRect();
    const planeWidth = plane.clientWidth || 960;
    const planeHeight = plane.clientHeight || 720;
    const scaledWidth = planeWidth * this.dungeonMapState.zoom;
    const scaledHeight = planeHeight * this.dungeonMapState.zoom;
    const maxPanX = Math.max(0, (scaledWidth - viewportRect.width) / 2 + 80);
    const maxPanY = Math.max(0, (scaledHeight - viewportRect.height) / 2 + 80);

    this.dungeonMapState.panX = Math.max(-maxPanX, Math.min(maxPanX, this.dungeonMapState.panX));
    this.dungeonMapState.panY = Math.max(-maxPanY, Math.min(maxPanY, this.dungeonMapState.panY));
  }

  applyDungeonMapTransform() {
    const plane = document.getElementById('dungeon-map-plane');
    if (!plane) return;
    const wasZoomed = plane.classList.contains('is-zoomed');
    const isZoomed = this.isDungeonMapZoomed();
    plane.style.transform = `translate(calc(-50% + ${this.dungeonMapState.panX}px), calc(-50% + ${this.dungeonMapState.panY}px)) scale(${this.dungeonMapState.zoom})`;
    plane.classList.toggle('is-zoomed', isZoomed);

    const label = document.getElementById('dungeon-map-zoom-label');
    if (label) label.textContent = `${Math.round(this.dungeonMapState.zoom * 100)}%`;

    if (wasZoomed !== isZoomed) {
      this.selectDungeonMapStage(this.dungeonMapState.selectedIndex, false);
    }
  }

  refreshAdventurePanel() {
    const container = document.getElementById('adventure-stages-grid');
    container.innerHTML = '';

    this.stages.forEach((stage, idx) => {
      const card = document.createElement('div');
      card.className = 'stage-card card';
      
      let isLocked = false;
      if (idx > 0) {
        const prevStage = this.stages[idx - 1];
        if (!prevStage.bossCleared) {
          isLocked = true;
        }
      }

      if (isLocked) card.classList.add('locked');
      
      const badgeText = stage.bossCleared ? '🛡️ 토벌 완료' : `권장 Lv.${stage.recommendedLevel}`;
      
      card.innerHTML = `
        <h3 class="stage-title">${stage.name}</h3>
        <p class="stage-desc">${stage.description}</p>
        <div class="stage-meta">
          <span>${stage.floors}개 구역</span>
          <span style="color: ${stage.bossCleared ? 'var(--success-color)' : 'var(--primary-color)'}">${badgeText}</span>
        </div>
        <button class="btn btn-primary" ${isLocked ? 'disabled' : ''}>던전 진입</button>
      `;

      const btn = card.querySelector('button');
      btn.onclick = () => this.enterDungeon(stage);

      container.appendChild(card);
    });
  }

  enterDungeon(stage) {
    if (this.player.hp <= 0) {
      alert('체력이 소진되었습니다. 마을에서 체력을 회복한 후 진입하세요!');
      return;
    }
    
    this.changeScreen('combat');
    this.combatManager.startCombat(this.player, stage, 1);
  }

  refreshShopPanel() {
    const container = document.getElementById('shop-items-grid');
    container.innerHTML = '';

    const recipes = window.getCraftingRecipeData?.(this.inventory) || [];
    if (!recipes.length) {
      container.innerHTML = `<div style="grid-column: 1/-1; color: var(--text-muted); padding: 20px;">조합 가능한 장비 등급 정보가 없습니다.</div>`;
      return;
    }

    recipes.forEach(recipe => {
      const card = document.createElement('div');
      card.className = 'shop-item card';

      const canCraft = recipe.ready && this.player.gold >= recipe.cost;

      card.innerHTML = `
        <div class="item-main-row">
          <div class="item-icon-placeholder ${recipe.toRarity.class}" style="border-color: ${recipe.toRarity.color}">⚗️</div>
          <div class="item-info">
            <h4 style="text-align:left; color:${recipe.toRarity.color}; font-size: 0.95rem;">${recipe.fromRarity.name} 장비 조합</h4>
            <div class="item-desc" style="margin-top: 3px;">${recipe.fromRarity.name} 장비 3개를 소모해 ${recipe.toRarity.name} 장비 1개를 만듭니다.</div>
            <div class="item-desc" style="margin-top: 3px;">보유: ${recipe.count}/3</div>
          </div>
        </div>
        <div class="shop-item-price">${recipe.cost} G</div>
        <button class="btn btn-secondary" ${canCraft ? '' : 'disabled'}>조합하기</button>
      `;

      card.querySelector('button').onclick = () => this.combineItems(recipe);
      container.appendChild(card);
    });
  }

  combineItems(recipe) {
    if (this.player.gold < recipe.cost) {
      alert('골드가 부족합니다!');
      return;
    }

    const ingredientIndexes = [];
    this.inventory.forEach((item, index) => {
      if (
        ingredientIndexes.length < 3 &&
        item.rarity?.id === recipe.from &&
        item.type !== window.ITEM_TYPES.POTION &&
        item.rarity?.id !== 'named'
      ) {
        ingredientIndexes.push(index);
      }
    });

    if (ingredientIndexes.length < 3) {
      alert('조합 재료가 부족합니다!');
      return;
    }

    ingredientIndexes.sort((a, b) => b - a).forEach(index => this.inventory.splice(index, 1));
    this.player.gold -= recipe.cost;
    let lastClearedIndex = -1;
    this.stages.forEach((stage, index) => {
      if (stage.bossCleared) lastClearedIndex = index;
    });
    const unlockedDungeon = lastClearedIndex + 2;
    const dungeonNumber = Math.min(Math.max(1, unlockedDungeon), this.stages.length);
    const crafted = window.generateRandomGear(this.player.classType, this.player.level, recipe.toRarity, dungeonNumber);
    crafted.id = `crafted_${Date.now()}_${Math.floor(Math.random() * 1000)}`;
    this.inventory.push(crafted);
    
    this.addGeneralLog(`⚗️ ${recipe.fromRarity.name} 장비 3개를 조합해 [${crafted.name}]을(를) 만들었습니다. (-${recipe.cost}골드)`);
    this.updateTownHUD();
    this.refreshShopPanel();
    this.refreshCharacterPanel();
    this.saveGame();
  }

  showGameClear() {
    document.getElementById('modal-game-clear').classList.add('active');
  }

  refreshForgePanel() {
    this.updateTownHUD();
    this.player.equipped = window.normalizeEquipped(this.player.equipped);
    const weaponSlot = this.player.equipped.weapon;
    const armorSlot = this.player.equipped.armor;
    const accessorySlot = this.player.equipped.accessory;
    const accessory2Slot = this.player.equipped.accessory2;
    const accessory3Slot = this.player.equipped.accessory3;
    const accessory4Slot = this.player.equipped.accessory4;

    this.renderForgeGear(weaponSlot, 'weapon', '🗡️ 무기 강화 대기석');
    this.renderForgeGear(armorSlot, 'armor', '🛡️ 방어구 강화 대기석');
    this.renderForgeGear(accessorySlot, 'accessory', '📿 액세서리 1 강화 대기석');
    this.renderForgeGear(accessory2Slot, 'accessory2', '📿 액세서리 2 강화 대기석');
    this.renderForgeGear(accessory3Slot, 'accessory3', '📿 액세서리 3 강화 대기석');
    this.renderForgeGear(accessory4Slot, 'accessory4', '📿 액세서리 4 강화 대기석');
  }

  renderForgeGear(gear, type, title) {
    const side = document.getElementById(`forge-${type}-side`);
    side.innerHTML = `<div class="forge-header">${title}</div>`;

    if (!gear) {
      side.innerHTML += `<div style="text-align: center; color: var(--text-muted); padding: 40px 0;">장착한 장비가 없습니다.</div>`;
      return;
    }

    const cost = window.getUpgradeCost(gear);
    const canAfford = this.player.gold >= cost;

    let comparisonHtml = '';
    const growth = 0.15;
    
    Object.keys(gear.stats).forEach(statName => {
      const val = gear.stats[statName];
      const isRate = window.isRateGearStat(statName);
      const rawNextVal = isRate ? Math.round(val * (1 + growth) * 100) / 100 : Math.round(val * (1 + growth));
      const nextVal = window.clampGearStatValue(statName, rawNextVal);
      const displayVal = window.formatGearStatValue(statName, val);
      const displayNextVal = window.formatGearStatValue(statName, nextVal);
      const koreanStatName = window.getGearStatLabel(statName);

      comparisonHtml += `
        <div class="compare-row">
          <span>${koreanStatName}</span>
          <span>${displayVal} <span class="compare-arrow">➔</span> ${displayNextVal}</span>
        </div>
      `;
    });

    side.innerHTML += `
      <div class="reward-item-box" style="margin: 12px 0;">
        <div class="item-icon-placeholder ${gear.rarity.class}">${this.getGearTypeIcon(type)}</div>
        <div style="flex-grow:1;">
          <div style="font-weight:bold; color: ${gear.rarity.color}">${gear.name} +${gear.upgradeLevel}</div>
          <div style="font-size:0.75rem; color:var(--text-muted)">${gear.description}</div>
        </div>
      </div>
      <div class="forge-stats-compare">
        <div style="font-size: 0.8rem; font-weight:bold; margin-bottom: 6px; color: var(--text-muted)">강화 성공률 80% / 성공 시 능력치 변동:</div>
        ${comparisonHtml}
      </div>
      <div style="display:flex; justify-content:space-between; align-items:center; margin-top: auto; padding-top: 14px;">
        <span style="font-weight:bold;">소모 비용: <span class="gold-display">${cost} G</span></span>
        <button class="btn btn-primary" ${canAfford ? '' : 'disabled'}>강화하기</button>
      </div>
    `;

    side.querySelector('button').onclick = () => this.executeForgeUpgrade(type, gear, cost);
  }

  executeForgeUpgrade(type, gear, cost) {
    if (this.player.gold < cost) {
      alert('골드가 부족합니다!');
      return;
    }

    this.player.gold -= cost;
    const oldName = `${gear.name} +${gear.upgradeLevel}`;
    const success = Math.random() < 0.8;

    if (success) {
      window.upgradeGear(gear);
      this.addGeneralLog(`⚒️ 대장간에서 [${oldName}]을(를) [**${gear.name} +${gear.upgradeLevel}**](으)로 강화 성공!`);
    } else {
      this.addGeneralLog(`💥 대장간에서 [${oldName}] 강화에 실패했습니다. 장비는 유지되었지만 **${cost}**골드가 소모되었습니다.`);
    }

    this.player.recalculateStats();
    
    this.refreshForgePanel();
    this.saveGame();
  }

  initEventListeners() {
    window.addEventListener('keydown', event => this.handleKeyboardAction(event));

    document.getElementById('btn-new-game').onclick = () => {
      const modal = document.getElementById('modal-char-create');
      const nameInput = document.getElementById('char-name-input');
      modal.classList.add('active');
      const content = modal.querySelector('.modal-content');
      if (content) content.scrollTop = 0;
      setTimeout(() => nameInput.focus(), 50);
    };
    
    document.getElementById('btn-load-game').onclick = () => {
      this.loadGame(this.activeSaveSlot);
    };

    const classCards = document.querySelectorAll('.class-card');
    let selectedClass = 'knight';
    
    classCards.forEach(card => {
      card.tabIndex = 0;
      card.setAttribute('role', 'button');
      card.setAttribute('data-keyboard-clickable', 'true');
      card.onclick = () => {
        classCards.forEach(c => c.classList.remove('selected'));
        card.classList.add('selected');
        selectedClass = card.dataset.class;
      };
    });

    document.getElementById('btn-submit-char').onclick = () => {
      const nameInput = document.getElementById('char-name-input');
      const name = nameInput.value.trim();
      
      if (!name) {
        alert('이름을 입력해 주세요!');
        return;
      }

      if (this.getSaveData(this.activeSaveSlot)) {
        const ok = confirm(`슬롯 ${this.activeSaveSlot}의 기존 저장 기록을 덮어쓰고 새 모험을 시작하시겠습니까?`);
        if (!ok) return;
      }

      document.getElementById('modal-char-create').classList.remove('active');
      this.startNewGame(name, selectedClass);
    };

    document.getElementById('btn-cancel-char').onclick = () => {
      document.getElementById('modal-char-create').classList.remove('active');
    };

    document.querySelectorAll('.town-tab-btn').forEach(btn => {
      btn.onclick = () => {
        this.openTownPanel(btn.dataset.panel);
      };
    });

    document.getElementById('btn-victory-confirm').onclick = () => {
      this.combatManager.claimVictory();
    };
    
    document.getElementById('btn-defeat-confirm').onclick = () => {
      this.combatManager.escapeDefeat();
    };

    document.getElementById('btn-clear-confirm').onclick = () => {
      document.getElementById('modal-game-clear').classList.remove('active');
      this.changeScreen('town');
      this.openTownPanel('adventure');
      this.saveGame();
    };

    document.getElementById('btn-town-save').onclick = () => {
      this.saveGame({ manual: true });
    };

    document.getElementById('btn-town-rest').onclick = () => {
      if (this.player.hp >= this.player.maxHp && this.player.mp >= this.player.maxMp) {
        alert('체력과 마나가 이미 가득 차 있습니다.');
        return;
      }
      
      const restCost = 25;
      if (this.player.gold < restCost) {
        alert('여관 휴식 비용(25골드)이 부족합니다!');
        return;
      }

      this.player.gold -= restCost;
      this.player.hp = this.player.maxHp;
      this.player.mp = this.player.maxMp;
      this.addGeneralLog(`💤 여관에서 휴식을 취하여 모든 체력과 마나를 회복했습니다! (-25골드)`);
      this.updateTownHUD();
      this.refreshCharacterPanel();
      this.saveGame();
    };

    document.getElementById('btn-reset-save').onclick = () => {
      if (confirm(`슬롯 ${this.activeSaveSlot}의 저장 데이터가 삭제됩니다. 정말 초기화하시겠습니까?`)) {
        localStorage.removeItem(this.getSaveKey());
        if (this.activeSaveSlot === 1) localStorage.removeItem(LEGACY_SAVE_KEY);
        this.checkSavedGame();
        alert(`슬롯 ${this.activeSaveSlot} 저장이 초기화되었습니다.`);
      }
    };
  }
}

window.addEventListener('load', () => {
  window.game = new GameManager();
});
