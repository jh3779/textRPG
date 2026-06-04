#include "../include/Player.h"

#include <algorithm>

Player::Player(const std::string& playerName)
    : name(playerName),
      hp(100),
      maxHp(100),
      attack(10),
      defense(3),
      level(1),
      experience(0),
      gold(0) {
}

std::string Player::getName() const {
    return name;
}

int Player::getHp() const {
    return hp;
}

int Player::getMaxHp() const {
    return maxHp;
}

int Player::getAttack() const {
    return attack;
}

int Player::getDefense() const {
    return defense;
}

int Player::getLevel() const {
    return level;
}

int Player::getExperience() const {
    return experience;
}

int Player::getGold() const {
    return gold;
}

void Player::setHp(int newHp) {
    hp = std::clamp(newHp, 0, maxHp);
}

void Player::setAttack(int newAttack) {
    attack = std::max(1, newAttack);
}

void Player::loadState(int savedHp, int savedMaxHp, int savedAttack, int savedDefense,
                       int savedLevel, int savedExperience, int savedGold) {
    maxHp = std::max(1, savedMaxHp);
    hp = std::clamp(savedHp, 0, maxHp);
    attack = std::max(1, savedAttack);
    defense = std::max(0, savedDefense);
    level = std::max(1, savedLevel);
    experience = std::max(0, savedExperience);
    gold = std::max(0, savedGold);
}

void Player::addExperience(int exp) {
    if (exp <= 0) {
        return;
    }

    experience += exp;
    while (experience >= level * 100) {
        experience -= level * 100;
        levelUp();
    }
}

void Player::addGold(int amount) {
    gold = std::max(0, gold + amount);
}

void Player::displayStatus() const {
    std::cout << "\n[플레이어 상태]\n"
              << "이름: " << name << "\n"
              << "레벨: " << level << "\n"
              << "HP: " << hp << "/" << maxHp << "\n"
              << "공격력: " << attack << "\n"
              << "방어력: " << defense << "\n"
              << "경험치: " << experience << "/" << level * 100 << "\n"
              << "골드: " << gold << "\n";
}

void Player::takeDamage(int damage) {
    int finalDamage = std::max(1, damage - defense);
    hp = std::max(0, hp - finalDamage);
    std::cout << name << "에게 " << finalDamage << " 피해.\n";
}

bool Player::isAlive() const {
    return hp > 0;
}

void Player::levelUp() {
    level++;
    maxHp += 20;
    attack += 3;
    defense += 1;
    hp = maxHp;

    std::cout << "\n레벨 업! 현재 레벨: " << level << "\n"
              << "최대 HP, 공격력, 방어력이 증가했습니다.\n";
}
