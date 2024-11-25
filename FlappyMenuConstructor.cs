using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;

namespace FlappyKnight {
    internal class FlappyMenuConstructor: ModeMenuConstructor {

        public override void OnEnterMainMenu(MenuPage modeMenu) {
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

            //UIManager.instance.StartNewGame();
            
            /*if(newGame) {
                UIManager.instance.StartNewGame();
            }
            else {
                UIManager.instance.ContinueGame();
                GameManager.instance.ContinueGame();
            }*/
        }
    }
}
