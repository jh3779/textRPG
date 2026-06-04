#include "../include/Enemy.h"

#include <algorithm>

Enemy::Enemy(const std::string& enemyName, int hp, int atk, int def, int exp, int gold)
    : name(enemyName),
      hp(hp),
      maxHp(hp),
      attack(atk),
      defense(def),
      experienceReward(exp),
      goldReward(gold) {
}

std::string Enemy::getName() const {
    return name;
}

int Enemy::getHp() const {
    return hp;
}

int Enemy::getMaxHp() const {
    return maxHp;
}

int Enemy::getAttack() const {
    return attack;
}

int Enemy::getDefense() const {
    return defense;
}

int Enemy::getExperienceReward() const {
    return experienceReward;
}

int Enemy::getGoldReward() const {
    return goldReward;
}

void Enemy::setHp(int newHp) {
    hp = std::clamp(newHp, 0, maxHp);
}

void Enemy::displayStatus() const {
    std::cout << "[적] " << name
              << " | HP: " << hp << "/" << maxHp
              << " | 공격력: " << attack
              << " | 방어력: " << defense << "\n";
}

void Enemy::takeDamage(int damage) {
    int finalDamage = std::max(1, damage - defense);
    hp = std::max(0, hp - finalDamage);
    std::cout << name << "에게 " << finalDamage << " 피해.\n";
}

bool Enemy::isAlive() const {
    return hp > 0;
}
