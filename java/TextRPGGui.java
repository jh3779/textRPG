import javax.swing.BorderFactory;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JOptionPane;
import javax.swing.JPanel;
import javax.swing.JScrollPane;
import javax.swing.JTextArea;
import javax.swing.SwingConstants;
import javax.swing.SwingUtilities;
import javax.swing.UIManager;
import java.awt.BasicStroke;
import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Dimension;
import java.awt.Font;
import java.awt.GradientPaint;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.RenderingHints;
import java.awt.event.ActionEvent;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Properties;
import java.util.Random;

@SuppressWarnings({"serial", "this-escape"})
public class TextRPGGui extends JFrame {
    private static final Path SAVE_PATH = Path.of("saves", "gui-save.properties");

    private final Random random = new Random();
    private final ScenePanel scenePanel = new ScenePanel();
    private final JTextArea storyArea = new JTextArea();
    private final JTextArea logArea = new JTextArea();
    private final JLabel statusLabel = new JLabel("", SwingConstants.LEFT);
    private final JButton[] choiceButtons = new JButton[3];
    private final JButton newGameButton = new JButton("새 게임");
    private final JButton loadButton = new JButton("이어하기");
    private final JButton saveButton = new JButton("저장");

    private Scene scene = Scene.MENU;
    private final Player player = new Player();
    private Enemy currentEnemy;
    private Scene sceneAfterWin;
    private Scene sceneAfterFlee;
    private int round = 0;
    private boolean armoryLooted = false;
    private boolean goblinDefeated = false;

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            try {
                UIManager.setLookAndFeel(UIManager.getSystemLookAndFeelClassName());
            } catch (Exception ignored) {
            }
            new TextRPGGui().setVisible(true);
        });
    }

    public TextRPGGui() {
        super("Console Text RPG - GUI");
        setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
        setMinimumSize(new Dimension(980, 680));

        JPanel root = new JPanel(new BorderLayout(12, 12));
        root.setBorder(BorderFactory.createEmptyBorder(14, 14, 14, 14));
        root.setBackground(new Color(24, 27, 31));

        JPanel header = new JPanel(new BorderLayout(10, 10));
        header.setOpaque(false);
        JLabel title = new JLabel("Console Text RPG");
        title.setForeground(new Color(238, 241, 245));
        title.setFont(new Font(Font.SANS_SERIF, Font.BOLD, 24));

        JPanel toolbar = new JPanel();
        toolbar.setOpaque(false);
        toolbar.add(newGameButton);
        toolbar.add(loadButton);
        toolbar.add(saveButton);

        header.add(title, BorderLayout.WEST);
        header.add(toolbar, BorderLayout.EAST);
        root.add(header, BorderLayout.NORTH);

        JPanel center = new JPanel(new BorderLayout(12, 12));
        center.setOpaque(false);
        center.add(scenePanel, BorderLayout.CENTER);
        center.add(buildRightPanel(), BorderLayout.EAST);
        root.add(center, BorderLayout.CENTER);

        logArea.setEditable(false);
        logArea.setLineWrap(true);
        logArea.setWrapStyleWord(true);
        logArea.setRows(5);
        logArea.setFont(new Font(Font.MONOSPACED, Font.PLAIN, 13));
        logArea.setBackground(new Color(18, 20, 23));
        logArea.setForeground(new Color(218, 224, 232));
        logArea.setBorder(BorderFactory.createEmptyBorder(10, 10, 10, 10));
        root.add(new JScrollPane(logArea), BorderLayout.SOUTH);

        setContentPane(root);
        bindActions();
        showMenu();
        pack();
        setLocationRelativeTo(null);
    }

    private JPanel buildRightPanel() {
        JPanel panel = new JPanel(new BorderLayout(10, 10));
        panel.setPreferredSize(new Dimension(360, 0));
        panel.setBackground(new Color(32, 36, 42));
        panel.setBorder(BorderFactory.createCompoundBorder(
            BorderFactory.createLineBorder(new Color(54, 61, 70)),
            BorderFactory.createEmptyBorder(12, 12, 12, 12)
        ));

        statusLabel.setForeground(new Color(225, 230, 238));
        statusLabel.setFont(new Font(Font.SANS_SERIF, Font.BOLD, 14));
        panel.add(statusLabel, BorderLayout.NORTH);

        storyArea.setEditable(false);
        storyArea.setLineWrap(true);
        storyArea.setWrapStyleWord(true);
        storyArea.setFont(new Font(Font.SANS_SERIF, Font.PLAIN, 16));
        storyArea.setBackground(new Color(32, 36, 42));
        storyArea.setForeground(new Color(238, 241, 245));
        storyArea.setBorder(BorderFactory.createEmptyBorder(8, 4, 8, 4));
        panel.add(new JScrollPane(storyArea), BorderLayout.CENTER);

        JPanel choices = new JPanel(new java.awt.GridLayout(3, 1, 8, 8));
        choices.setOpaque(false);
        for (int i = 0; i < choiceButtons.length; i++) {
            JButton button = new JButton();
            button.setFont(new Font(Font.SANS_SERIF, Font.BOLD, 15));
            button.setFocusPainted(false);
            choiceButtons[i] = button;
            choices.add(button);
        }
        panel.add(choices, BorderLayout.SOUTH);
        return panel;
    }

    private void bindActions() {
        newGameButton.addActionListener(event -> startNewGame());
        loadButton.addActionListener(event -> loadGame());
        saveButton.addActionListener(event -> saveGame());

        for (int i = 0; i < choiceButtons.length; i++) {
            final int choice = i + 1;
            choiceButtons[i].addActionListener((ActionEvent event) -> handleChoice(choice));
        }
    }

    private void showMenu() {
        scene = Scene.MENU;
        currentEnemy = null;
        updateScreen(
            "메인 메뉴",
            "새 게임을 시작하거나 저장된 GUI 세이브를 불러올 수 있습니다.\n\n" +
                "오른쪽 선택 버튼을 누르면 바로 진행됩니다.",
            "새 게임",
            "이어하기",
            "종료"
        );
        saveButton.setEnabled(false);
    }

    private void startNewGame() {
        player.reset();
        round = 1;
        armoryLooted = false;
        goblinDefeated = false;
        currentEnemy = null;
        logArea.setText("");
        appendLog("새 게임을 시작했습니다.");
        goTo(Scene.ENTRANCE, false);
    }

    private void goTo(Scene nextScene) {
        goTo(nextScene, true);
    }

    private void goTo(Scene nextScene, boolean advanceRound) {
        scene = nextScene;
        currentEnemy = null;
        if (advanceRound) {
            round++;
        }
        renderScene();
    }

    private void renderScene() {
        switch (scene) {
            case ENTRANCE:
                updateScreen(
                    "던전 입구",
                    "차가운 바람이 던전 안쪽에서 새어 나옵니다.\n" +
                        "입구의 횃불은 거의 꺼져가고, 발밑에는 오래된 발자국이 이어져 있습니다.",
                    "던전에 들어간다",
                    "상태 확인",
                    "저장하고 종료"
                );
                break;
            case FORK:
                updateScreen(
                    "갈림길",
                    "왼쪽 통로에는 희미한 빛이 보이고, 오른쪽 통로에서는 낮은 울음소리가 들립니다.\n" +
                        "어느 쪽으로 갈지 선택해야 합니다.",
                    "무기고로 간다",
                    "어두운 통로로 간다",
                    "저장하고 종료"
                );
                break;
            case ARMORY:
                if (armoryLooted) {
                    updateScreen(
                        "낡은 무기고",
                        "무기고는 이미 비어 있습니다. 쓸 만한 장비는 모두 챙겼습니다.",
                        "어두운 통로로 간다",
                        "갈림길로 돌아간다",
                        "저장하고 종료"
                    );
                } else {
                    updateScreen(
                        "낡은 무기고",
                        "먼지 쌓인 상자 사이에서 낡은 검과 작은 회복 물약을 발견했습니다.\n" +
                            "장비를 챙기면 앞으로의 전투가 조금 쉬워질 것 같습니다.",
                        "장비를 챙긴다",
                        "그냥 지나간다",
                        "저장하고 종료"
                    );
                }
                break;
            case GOBLIN:
                if (goblinDefeated) {
                    goTo(Scene.GOBLIN_CLEAR, false);
                } else {
                    beginBattle(
                        new Enemy("고블린", 30, 7, 1, 60, 25),
                        Scene.BOSS,
                        Scene.FORK,
                        "어두운 통로에서 고블린이 튀어나왔습니다."
                    );
                }
                break;
            case GOBLIN_CLEAR:
                updateScreen(
                    "어두운 통로",
                    "고블린은 이미 쓰러졌고 통로는 조용합니다.\n" +
                        "안쪽에서는 더 강한 기척이 느껴집니다.",
                    "보스의 방으로 간다",
                    "갈림길로 돌아간다",
                    "저장하고 종료"
                );
                break;
            case BOSS:
                updateScreen(
                    "보스의 방",
                    "거대한 문 너머에서 던전 수호자의 발소리가 울립니다.\n" +
                        "전투를 시작하기 전까지는 저장할 수 있습니다.",
                    "보스에게 도전한다",
                    "갈림길로 물러난다",
                    "저장하고 종료"
                );
                break;
            case VICTORY:
                updateScreen(
                    "던전 클리어",
                    "던전 수호자를 쓰러뜨렸습니다.\n" +
                        "어둡던 던전 끝에서 바깥의 빛이 보입니다.",
                    "새 게임",
                    "메인 메뉴",
                    "종료"
                );
                saveButton.setEnabled(false);
                break;
            case GAME_OVER:
                updateScreen(
                    "게임 오버",
                    "체력이 0이 되어 모험이 끝났습니다.",
                    "새 게임",
                    "메인 메뉴",
                    "종료"
                );
                saveButton.setEnabled(false);
                break;
            case MENU:
            case BATTLE:
                break;
        }
    }

    private void beginBattle(Enemy enemy, Scene winScene, Scene fleeScene, String intro) {
        currentEnemy = enemy;
        sceneAfterWin = winScene;
        sceneAfterFlee = fleeScene;
        scene = Scene.BATTLE;
        appendLog(intro);
        renderBattle();
    }

    private void renderBattle() {
        String enemyStatus = currentEnemy.name + " HP " + currentEnemy.hp + "/" + currentEnemy.maxHp;
        updateScreen(
            "전투",
            enemyStatus + "\n\n" +
                currentEnemy.name + "이(가) 앞을 막고 있습니다.\n" +
                "공격하거나 도망을 시도하세요. 전투 중에는 저장할 수 없습니다.",
            "공격한다",
            "도망친다",
            ""
        );
        choiceButtons[2].setEnabled(false);
        saveButton.setEnabled(false);
    }

    private void handleChoice(int choice) {
        switch (scene) {
            case MENU:
                if (choice == 1) {
                    startNewGame();
                } else if (choice == 2) {
                    loadGame();
                } else {
                    dispose();
                }
                break;
            case ENTRANCE:
                if (choice == 1) {
                    goTo(Scene.FORK);
                } else if (choice == 2) {
                    showStatusDialog();
                } else {
                    saveAndReturnToMenu();
                }
                break;
            case FORK:
                if (choice == 1) {
                    goTo(Scene.ARMORY);
                } else if (choice == 2) {
                    goTo(Scene.GOBLIN);
                } else {
                    saveAndReturnToMenu();
                }
                break;
            case ARMORY:
                if (choice == 1 && !armoryLooted) {
                    armoryLooted = true;
                    player.attack += 4;
                    appendLog("낡은 검을 장비했습니다. 공격력 +4");
                    goTo(Scene.GOBLIN);
                } else if (choice == 1) {
                    goTo(Scene.GOBLIN);
                } else if (choice == 2) {
                    goTo(armoryLooted ? Scene.FORK : Scene.GOBLIN);
                } else {
                    saveAndReturnToMenu();
                }
                break;
            case GOBLIN_CLEAR:
                if (choice == 1) {
                    goTo(Scene.BOSS);
                } else if (choice == 2) {
                    goTo(Scene.FORK);
                } else {
                    saveAndReturnToMenu();
                }
                break;
            case BOSS:
                if (choice == 1) {
                    beginBattle(
                        new Enemy("던전 수호자", 55, 10, 3, 120, 70),
                        Scene.VICTORY,
                        Scene.GOBLIN_CLEAR,
                        "던전 수호자가 거대한 무기를 들어 올립니다."
                    );
                } else if (choice == 2) {
                    goTo(Scene.FORK);
                } else {
                    saveAndReturnToMenu();
                }
                break;
            case BATTLE:
                if (choice == 1) {
                    attackEnemy();
                } else if (choice == 2) {
                    fleeBattle();
                }
                break;
            case VICTORY:
            case GAME_OVER:
                if (choice == 1) {
                    startNewGame();
                } else if (choice == 2) {
                    showMenu();
                } else {
                    dispose();
                }
                break;
            default:
                break;
        }
    }

    private void attackEnemy() {
        int playerDamage = Math.max(1, player.attack + random.nextInt(4) - currentEnemy.defense);
        currentEnemy.hp = Math.max(0, currentEnemy.hp - playerDamage);
        appendLog("모험가의 공격: " + currentEnemy.name + "에게 " + playerDamage + " 피해.");

        if (currentEnemy.hp <= 0) {
            winBattle();
            return;
        }

        int enemyDamage = Math.max(1, currentEnemy.attack + random.nextInt(3) - player.defense);
        player.hp = Math.max(0, player.hp - enemyDamage);
        appendLog(currentEnemy.name + "의 반격: 모험가에게 " + enemyDamage + " 피해.");

        if (player.hp <= 0) {
            goTo(Scene.GAME_OVER);
        } else {
            renderBattle();
        }
    }

    private void winBattle() {
        appendLog(currentEnemy.name + " 처치. 경험치 " + currentEnemy.expReward + ", 골드 " + currentEnemy.goldReward + " 획득.");
        player.addExperience(currentEnemy.expReward);
        player.gold += currentEnemy.goldReward;

        if ("고블린".equals(currentEnemy.name)) {
            goblinDefeated = true;
        }

        if (sceneAfterWin == Scene.VICTORY) {
            appendLog("퀘스트 완료: 던전 탈출. 추가 보상 경험치 100, 골드 80 획득.");
            player.addExperience(100);
            player.gold += 80;
        }

        goTo(sceneAfterWin);
    }

    private void fleeBattle() {
        if (random.nextInt(100) < 55) {
            appendLog("도망에 성공했습니다.");
            goTo(sceneAfterFlee);
        } else {
            appendLog("도망에 실패했습니다.");
            int enemyDamage = Math.max(1, currentEnemy.attack + random.nextInt(3) - player.defense);
            player.hp = Math.max(0, player.hp - enemyDamage);
            appendLog(currentEnemy.name + "의 추격: 모험가에게 " + enemyDamage + " 피해.");

            if (player.hp <= 0) {
                goTo(Scene.GAME_OVER);
            } else {
                renderBattle();
            }
        }
    }

    private void showStatusDialog() {
        JOptionPane.showMessageDialog(
            this,
            "레벨: " + player.level + "\n" +
                "HP: " + player.hp + "/" + player.maxHp + "\n" +
                "공격력: " + player.attack + "\n" +
                "방어력: " + player.defense + "\n" +
                "경험치: " + player.experience + "/" + (player.level * 100) + "\n" +
                "골드: " + player.gold + "\n\n" +
                "인벤토리\n" +
                "- 회복 물약\n" +
                (armoryLooted ? "- 작은 회복 물약\n" : ""),
            "플레이어 상태",
            JOptionPane.INFORMATION_MESSAGE
        );
    }

    private void updateScreen(String title, String story, String first, String second, String third) {
        statusLabel.setText(buildStatusText(title));
        storyArea.setText(story);
        storyArea.setCaretPosition(0);
        setChoice(0, first);
        setChoice(1, second);
        setChoice(2, third);
        saveButton.setEnabled(canSave());
        scenePanel.repaint();
    }

    private String buildStatusText(String title) {
        String enemyText = "";
        if (currentEnemy != null && scene == Scene.BATTLE) {
            enemyText = " | " + currentEnemy.name + " HP " + currentEnemy.hp + "/" + currentEnemy.maxHp;
        }

        return "<html><b>" + title + "</b><br>" +
            "라운드 " + round +
            " | LV " + player.level +
            " | HP " + player.hp + "/" + player.maxHp +
            " | ATK " + player.attack +
            " | Gold " + player.gold +
            enemyText +
            "</html>";
    }

    private void setChoice(int index, String text) {
        JButton button = choiceButtons[index];
        button.setText(text == null ? "" : text);
        button.setEnabled(text != null && !text.isBlank());
    }

    private boolean canSave() {
        return scene != Scene.MENU && scene != Scene.BATTLE && scene != Scene.VICTORY && scene != Scene.GAME_OVER;
    }

    private void saveAndReturnToMenu() {
        if (saveGame()) {
            appendLog("저장했습니다: " + SAVE_PATH);
            showMenu();
        }
    }

    private boolean saveGame() {
        if (!canSave()) {
            JOptionPane.showMessageDialog(this, "현재 장면에서는 저장할 수 없습니다.", "저장 불가", JOptionPane.WARNING_MESSAGE);
            return false;
        }

        Properties properties = new Properties();
        properties.setProperty("version", "1");
        properties.setProperty("scene", scene.name());
        properties.setProperty("round", String.valueOf(round));
        properties.setProperty("hp", String.valueOf(player.hp));
        properties.setProperty("maxHp", String.valueOf(player.maxHp));
        properties.setProperty("attack", String.valueOf(player.attack));
        properties.setProperty("defense", String.valueOf(player.defense));
        properties.setProperty("level", String.valueOf(player.level));
        properties.setProperty("experience", String.valueOf(player.experience));
        properties.setProperty("gold", String.valueOf(player.gold));
        properties.setProperty("armoryLooted", String.valueOf(armoryLooted));
        properties.setProperty("goblinDefeated", String.valueOf(goblinDefeated));

        try {
            Files.createDirectories(SAVE_PATH.getParent());
            try (OutputStream output = Files.newOutputStream(SAVE_PATH)) {
                properties.store(output, "Text RPG GUI Save");
            }
            return true;
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(this, "저장 실패: " + ex.getMessage(), "오류", JOptionPane.ERROR_MESSAGE);
            return false;
        }
    }

    private void loadGame() {
        if (!Files.exists(SAVE_PATH)) {
            JOptionPane.showMessageDialog(this, "저장 파일이 없습니다.", "이어하기", JOptionPane.INFORMATION_MESSAGE);
            return;
        }

        Properties properties = new Properties();
        try (InputStream input = Files.newInputStream(SAVE_PATH)) {
            properties.load(input);
        } catch (IOException ex) {
            JOptionPane.showMessageDialog(this, "불러오기 실패: " + ex.getMessage(), "오류", JOptionPane.ERROR_MESSAGE);
            return;
        }

        if (!"1".equals(properties.getProperty("version"))) {
            JOptionPane.showMessageDialog(this, "지원하지 않는 저장 파일입니다.", "오류", JOptionPane.ERROR_MESSAGE);
            return;
        }

        player.hp = readInt(properties, "hp", 100);
        player.maxHp = readInt(properties, "maxHp", 100);
        player.attack = readInt(properties, "attack", 10);
        player.defense = readInt(properties, "defense", 3);
        player.level = readInt(properties, "level", 1);
        player.experience = readInt(properties, "experience", 0);
        player.gold = readInt(properties, "gold", 0);
        round = readInt(properties, "round", 1);
        armoryLooted = Boolean.parseBoolean(properties.getProperty("armoryLooted", "false"));
        goblinDefeated = Boolean.parseBoolean(properties.getProperty("goblinDefeated", "false"));

        try {
            scene = Scene.valueOf(properties.getProperty("scene", Scene.ENTRANCE.name()));
        } catch (IllegalArgumentException ex) {
            scene = Scene.ENTRANCE;
        }

        currentEnemy = null;
        appendLog("저장된 게임을 불러왔습니다.");
        renderScene();
    }

    private int readInt(Properties properties, String key, int fallback) {
        try {
            return Integer.parseInt(properties.getProperty(key, String.valueOf(fallback)));
        } catch (NumberFormatException ex) {
            return fallback;
        }
    }

    private void appendLog(String text) {
        logArea.append(text + "\n");
        logArea.setCaretPosition(logArea.getDocument().getLength());
    }

    private enum Scene {
        MENU,
        ENTRANCE,
        FORK,
        ARMORY,
        GOBLIN,
        GOBLIN_CLEAR,
        BOSS,
        BATTLE,
        VICTORY,
        GAME_OVER
    }

    private static final class Player {
        private int hp;
        private int maxHp;
        private int attack;
        private int defense;
        private int level;
        private int experience;
        private int gold;

        private Player() {
            reset();
        }

        private void reset() {
            hp = 100;
            maxHp = 100;
            attack = 10;
            defense = 3;
            level = 1;
            experience = 0;
            gold = 0;
        }

        private void addExperience(int amount) {
            experience += Math.max(0, amount);
            while (experience >= level * 100) {
                experience -= level * 100;
                level++;
                maxHp += 20;
                attack += 3;
                defense += 1;
                hp = maxHp;
            }
        }
    }

    private static final class Enemy {
        private final String name;
        private int hp;
        private final int maxHp;
        private final int attack;
        private final int defense;
        private final int expReward;
        private final int goldReward;

        private Enemy(String name, int hp, int attack, int defense, int expReward, int goldReward) {
            this.name = name;
            this.hp = hp;
            this.maxHp = hp;
            this.attack = attack;
            this.defense = defense;
            this.expReward = expReward;
            this.goldReward = goldReward;
        }
    }

    private final class ScenePanel extends JPanel {
        private ScenePanel() {
            setPreferredSize(new Dimension(560, 420));
            setBackground(new Color(14, 18, 24));
            setBorder(BorderFactory.createLineBorder(new Color(54, 61, 70)));
        }

        @Override
        protected void paintComponent(Graphics graphics) {
            super.paintComponent(graphics);
            Graphics2D g = (Graphics2D) graphics.create();
            g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);

            drawBackdrop(g);
            drawScene(g);

            g.dispose();
        }

        private void drawBackdrop(Graphics2D g) {
            int width = getWidth();
            int height = getHeight();
            Color top = scene == Scene.VICTORY ? new Color(51, 95, 119) : new Color(18, 22, 30);
            Color bottom = scene == Scene.GAME_OVER ? new Color(63, 28, 30) : new Color(7, 10, 14);
            g.setPaint(new GradientPaint(0, 0, top, 0, height, bottom));
            g.fillRect(0, 0, width, height);

            g.setColor(new Color(0, 0, 0, 80));
            g.fillRect(0, height - 92, width, 92);
        }

        private void drawScene(Graphics2D g) {
            switch (scene) {
                case MENU:
                    drawEntrance(g, "GUI");
                    break;
                case ENTRANCE:
                    drawEntrance(g, "입구");
                    break;
                case FORK:
                    drawFork(g);
                    break;
                case ARMORY:
                    drawArmory(g);
                    break;
                case GOBLIN:
                case BATTLE:
                    if (currentEnemy != null && "던전 수호자".equals(currentEnemy.name)) {
                        drawBoss(g);
                    } else {
                        drawGoblin(g);
                    }
                    break;
                case GOBLIN_CLEAR:
                    drawClearPassage(g);
                    break;
                case BOSS:
                    drawBossDoor(g);
                    break;
                case VICTORY:
                    drawVictory(g);
                    break;
                case GAME_OVER:
                    drawGameOver(g);
                    break;
            }
        }

        private void drawEntrance(Graphics2D g, String label) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(32, 34, 38));
            g.fillRoundRect(w / 2 - 145, h / 2 - 120, 290, 250, 30, 30);
            g.setColor(new Color(8, 10, 14));
            g.fillRoundRect(w / 2 - 98, h / 2 - 62, 196, 190, 90, 90);
            drawTorch(g, w / 2 - 170, h / 2 - 35);
            drawTorch(g, w / 2 + 155, h / 2 - 35);
            drawCaption(g, "던전 " + label);
        }

        private void drawFork(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(26, 28, 34));
            g.fillPolygon(new int[]{w / 2 - 45, 80, 210}, new int[]{h - 80, 110, h - 80}, 3);
            g.fillPolygon(new int[]{w / 2 + 45, w - 80, w - 210}, new int[]{h - 80, 110, h - 80}, 3);
            g.setColor(new Color(188, 168, 89, 170));
            g.fillOval(105, 95, 54, 54);
            g.setColor(new Color(90, 105, 122));
            g.fillOval(w - 150, 110, 42, 42);
            drawCaption(g, "갈림길");
        }

        private void drawArmory(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(72, 52, 39));
            g.fillRoundRect(w / 2 - 150, h / 2 - 60, 300, 145, 12, 12);
            g.setColor(new Color(142, 105, 58));
            g.fillRect(w / 2 - 130, h / 2 - 38, 260, 30);
            g.setStroke(new BasicStroke(7));
            g.setColor(new Color(190, 194, 189));
            g.drawLine(w / 2 - 70, h / 2 + 85, w / 2 + 80, h / 2 - 85);
            g.setStroke(new BasicStroke(1));
            g.setColor(new Color(79, 96, 116));
            g.fillOval(w / 2 + 84, h / 2 - 102, 42, 42);
            drawCaption(g, armoryLooted ? "비어 있는 무기고" : "낡은 무기고");
        }

        private void drawGoblin(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(32, 45, 38));
            g.fillOval(w / 2 - 72, h / 2 - 54, 144, 125);
            g.setColor(new Color(86, 134, 83));
            g.fillOval(w / 2 - 64, h / 2 - 105, 128, 102);
            g.setColor(new Color(19, 24, 21));
            g.fillOval(w / 2 - 30, h / 2 - 64, 16, 18);
            g.fillOval(w / 2 + 14, h / 2 - 64, 16, 18);
            g.setColor(new Color(178, 76, 61));
            g.fillArc(w / 2 - 30, h / 2 - 38, 60, 34, 200, 140);
            g.setColor(new Color(70, 101, 69));
            g.fillPolygon(new int[]{w / 2 - 58, w / 2 - 125, w / 2 - 57}, new int[]{h / 2 - 75, h / 2 - 104, h / 2 - 34}, 3);
            g.fillPolygon(new int[]{w / 2 + 58, w / 2 + 125, w / 2 + 57}, new int[]{h / 2 - 75, h / 2 - 104, h / 2 - 34}, 3);
            drawCaption(g, "고블린");
        }

        private void drawClearPassage(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(22, 24, 30));
            g.fillRoundRect(w / 2 - 120, 80, 240, h - 145, 120, 120);
            g.setColor(new Color(80, 88, 96));
            g.fillOval(w / 2 - 95, h - 130, 190, 22);
            drawCaption(g, "조용한 통로");
        }

        private void drawBossDoor(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(62, 51, 47));
            g.fillRoundRect(w / 2 - 150, h / 2 - 135, 300, 260, 22, 22);
            g.setColor(new Color(98, 76, 54));
            g.fillRoundRect(w / 2 - 124, h / 2 - 110, 248, 235, 14, 14);
            g.setColor(new Color(196, 150, 64));
            g.fillOval(w / 2 + 86, h / 2 + 4, 20, 20);
            drawCaption(g, "보스의 방");
        }

        private void drawBoss(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(55, 55, 65));
            g.fillRoundRect(w / 2 - 90, h / 2 - 100, 180, 210, 36, 36);
            g.setColor(new Color(92, 91, 104));
            g.fillOval(w / 2 - 80, h / 2 - 155, 160, 125);
            g.setColor(new Color(219, 83, 68));
            g.fillOval(w / 2 - 42, h / 2 - 107, 22, 22);
            g.fillOval(w / 2 + 20, h / 2 - 107, 22, 22);
            g.setStroke(new BasicStroke(9));
            g.setColor(new Color(161, 163, 166));
            g.drawLine(w / 2 + 98, h / 2 - 10, w / 2 + 176, h / 2 - 110);
            g.setStroke(new BasicStroke(1));
            drawCaption(g, "던전 수호자");
        }

        private void drawVictory(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(238, 206, 111, 190));
            g.fillOval(w / 2 - 105, 68, 210, 210);
            g.setColor(new Color(48, 67, 80));
            g.fillRoundRect(w / 2 - 120, h / 2 + 12, 240, 120, 18, 18);
            g.setColor(new Color(218, 232, 229));
            g.fillRect(w / 2 - 18, h / 2 - 40, 36, 152);
            drawCaption(g, "승리");
        }

        private void drawGameOver(Graphics2D g) {
            int w = getWidth();
            int h = getHeight();
            g.setColor(new Color(96, 42, 44));
            g.fillOval(w / 2 - 82, h / 2 - 95, 164, 164);
            g.setColor(new Color(20, 20, 22));
            g.fillRect(w / 2 - 96, h / 2 + 52, 192, 55);
            drawCaption(g, "GAME OVER");
        }

        private void drawTorch(Graphics2D g, int x, int y) {
            g.setColor(new Color(114, 72, 38));
            g.fillRect(x, y + 38, 14, 82);
            g.setColor(new Color(226, 92, 46));
            g.fillOval(x - 15, y, 44, 54);
            g.setColor(new Color(247, 188, 71));
            g.fillOval(x - 5, y + 8, 24, 34);
        }

        private void drawCaption(Graphics2D g, String text) {
            g.setFont(new Font(Font.SANS_SERIF, Font.BOLD, 28));
            g.setColor(new Color(238, 241, 245));
            int textWidth = g.getFontMetrics().stringWidth(text);
            g.drawString(text, (getWidth() - textWidth) / 2, getHeight() - 36);
        }
    }
}
