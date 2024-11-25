using MenuChanger;
using MenuChanger.MenuElements;

namespace FlappyKnight {
    internal class FlappyMenuConstructor: ModeMenuConstructor {

        public override void OnEnterMainMenu(MenuPage modeMenu) {
            FlappyKnight.instance.isFlappyMode = false;
        }

        public override void OnExitMainMenu() {
        }

        public override bool TryGetModeButton(MenuPage modeMenu, out BigButton button) {
            button = new BigButton(modeMenu, "Flappy Knight");
            button.OnClick += () => StartGame();
            return true;
        }

        private static void StartGame() {
            MenuChangerMod.HideAllMenuPages();
            FlappyKnight.instance.isFlappyMode = true;

            UIManager.instance.ContinueGame();
            GameManager.instance.ContinueGame();
        }
    }
}
