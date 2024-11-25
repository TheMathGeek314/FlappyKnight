using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using HutongGames.PlayMaker;
using GlobalEnums;
using MenuChanger;
using Satchel;

namespace FlappyKnight {
    public class FlappyKnight: Mod {
        new public string GetName() => "FlappyKnight";
        public override string GetVersion() => "1.0.0.0";

        public static FlappyKnight instance;

        public static GameObject goamLowPrefab;
        public static GameObject goamHighPrefab;
        public static GameObject leverPrefab;

        public bool isFlappyMode;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            On.GameManager.OnNextLevelReady += sceneChange;
            On.GameManager.ReadyForRespawn += readyForRespawn;
            On.HeroController.LocateSpawnPoint += locateSpawnPoint;
            On.HeroController.CanDoubleJump += canDoubleJump;
            On.HeroController.StartRecoil += startRecoil;
            On.GameManager.ReturnToMainMenu += returnToMainMenu;
            goamLowPrefab = preloadedObjects["Crossroads_13"]["_Enemies/Worm (11)"];
            goamHighPrefab = preloadedObjects["Crossroads_13"]["_Enemies/Worm (12)"];
            leverPrefab = preloadedObjects["Ruins1_31"]["Lift Call Lever"];
            instance = this;
            ModeMenu.AddMode(new FlappyMenuConstructor());

            //testing
            ModHooks.HeroUpdateHook += heroUpdate;
        }

        private void heroUpdate() {
            if(Input.GetKeyDown(KeyCode.O)) {
                runGoamSpawning();
            }
        }

        public override List<(string, string)> GetPreloadNames() {
            return new List<(string, string)> {
                ("Crossroads_13", "_Enemies/Worm (11)"),
                ("Crossroads_13", "_Enemies/Worm (12)"),
                ("Ruins1_31", "Lift Call Lever")
            };
        }

        private Transform locateSpawnPoint(On.HeroController.orig_LocateSpawnPoint orig, HeroController self) {
            if(orig(self) == null && isFlappyMode) {
                return GameObject.Find("top1").transform;
            }
            else {
                return orig(self);
            }
        }

        private void readyForRespawn(On.GameManager.orig_ReadyForRespawn orig, GameManager self, bool isFirstLevelForPlayer) {
            if(isFirstLevelForPlayer && isFlappyMode) {
                self.RespawningHero = true;
                self.BeginSceneTransition(new GameManager.SceneLoadInfo {
                    PreventCameraFadeOut = true,
                    WaitForSceneTransitionCameraFade = false,
                    EntryGateName = "top1",
                    SceneName = "Room_Sly_Storeroom",
                    Visualization = GameManager.SceneLoadVisualizations.ContinueFromSave,
                    AlwaysUnloadUnusedAssets = true,
                    IsFirstLevelForPlayer = true
                });
            }
            else {
                orig(self, isFirstLevelForPlayer);
            }
        }

        private void sceneChange(On.GameManager.orig_OnNextLevelReady orig, GameManager self) {
            orig(self);
            if(instance.isFlappyMode && self.sceneName == "Room_Sly_Storeroom") {
                GameObject[] gos = GameObject.FindObjectsOfType<GameObject>();
                List<GameObject> toDestroy = new();
                foreach(GameObject go in gos) {
                    foreach(string key in new List<string>() { "Sly_Storeroom", "nailmaster_03_cloth", "Candle", "wall collider", "Roof Collider", "haze2", "Shop Item" }) {
                        if(go.name.Contains(key) && !toDestroy.Contains(go)) {
                            toDestroy.Add(go);
                        }
                    }
                    if(new List<string>() {"Walk Area", "Sly Basement NPC", "rope", "Dream Dialogue", "sly_beam_glow", "door1"}.Contains(go.name)) {
                        toDestroy.Add(go);
                    }
                    if(go.name.Contains("Chunk 0 ")) {
                        if(go.name != "Chunk 0 0") {
                            toDestroy.Add(go);
                        }
                    }
                }
                for(int i = 0; i < toDestroy.Count; i++) {
                    GameObject.Destroy(toDestroy[i]);
                }
                spawnStartLever();
                HeroController.instance.gameObject.transform.SetPosition2D(15, 8);
                HeroController.instance.FaceRight();
                GameObject transition = GameObject.Find("top1");
                transition.GetComponent<BoxCollider2D>().isTrigger = false;
                transition.layer = LayerMask.NameToLayer("Terrain");
                GameObject.Instantiate(GameObject.Find("Chunk 0 0"), new Vector3(31, 0, 0), Quaternion.identity).transform.SetScaleX(-1);
            }
        }

        private bool canDoubleJump(On.HeroController.orig_CanDoubleJump orig, HeroController self) {
            if(FlappyKnight.instance.isFlappyMode)
                return true;
            return orig(self);
        }

        private IEnumerator startRecoil(On.HeroController.orig_StartRecoil orig, HeroController self, CollisionSide impactSide, bool spawnDamageEffect, int damageAmount) {
            if(isFlappyMode) {
                return null;
            }
            else {
                return orig(self, impactSide, spawnDamageEffect, damageAmount);
            }
        }

        private IEnumerator returnToMainMenu(On.GameManager.orig_ReturnToMainMenu orig, GameManager self, GameManager.ReturnToMainMenuSaveModes saveMode, Action<bool> callback) {
            if(isFlappyMode) {
                GameManager.instance.ClearSaveFile(GameManager.instance.profileID, delegate (bool didClear) {});
                saveMode = GameManager.ReturnToMainMenuSaveModes.DontSave;
            }
            isFlappyMode = false;
            return orig(self, saveMode, callback);
        }

        private void spawnStartLever() {
            GameObject lever = GameObject.Instantiate(leverPrefab, new Vector3(15.6f, 14.6f, 0.0072f), Quaternion.Euler(0, 0, 180));
            PlayMakerFSM self = lever.GetComponent<PlayMakerFSM>();
            self.ChangeTransition("Get Direction", "RIGHT", "Left");
            self.ChangeTransition("Get Direction", "LEFT", "Right");
            FsmState sendStart = self.AddState("Send Start");
            self.ChangeTransition("Left", "FINISHED", "Send Start");
            self.ChangeTransition("Right", "FINISHED", "Send Start");
            sendStart.AddTransition("FINISHED", "Idle");
            sendStart.AddAction(new SendGoamStart());
            lever.SetActive(true);
        }

        public async void runGoamSpawning() {
            float interval = 1.5f;
            float squeeze = 0.01f;
            float speed = 8;
            float acceleration = 0.1f;
            float offset = 9;
            float squish = 0.02f;
            float knightX = 25;

            System.Random rand = new();
            List<(GameObject, GameObject)> goamPairs = new();
            PlayerData pd = PlayerData.instance;
            GameObject knight = HeroController.instance.gameObject;
            Rigidbody2D rb = knight.GetComponent<Rigidbody2D>();
            float lastSpawnTime = 0;
            foreach(string name in new string[] { "Chunk 0 0", "Chunk 0 0(Clone)", "top1", "Lift Call Lever(Clone)(Clone)" }) {
                GameObject.Find(name).SetActive(false);
            }
            knight.transform.SetPositionX(knightX);
            knight.transform.SetPositionY(16);
            rb.velocity.Set(0, 0);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

            while(true) {
                if(GameManager.instance.isPaused) {
                    lastSpawnTime += Time.deltaTime;
                }
                else {
                    if(knight.transform.position.y > 20 || knight.transform.position.y < -1) {
                        endGame();
                        break;
                    }
                    if(Time.time - lastSpawnTime > interval) {
                        speed += acceleration;
                        offset -= squish;
                        interval -= squeeze;
                        double height = rand.NextDouble() * 10 + 5;
                        GameObject top = spawnGoam(true, height, offset);
                        GameObject bottom = spawnGoam(false, height, offset);
                        top.SetActive(true);
                        bottom.SetActive(true);
                        goamPairs.Add((top, bottom));
                        lastSpawnTime = Time.time;
                    }
                    float delta = Time.deltaTime;
                    foreach((GameObject, GameObject) pair in goamPairs) {
                        float oldX = pair.Item1.transform.position.x;
                        float newX = oldX - delta*speed;
                        pair.Item1.transform.SetPositionX(newX);
                        pair.Item2.transform.SetPositionX(newX);
                        bool wasRight = oldX > knightX;
                        bool isLeft = newX <= knightX;
                        if(wasRight && isLeft) {
                            HeroController.instance.AddGeo(1);
                        }
                    }
                    if(goamPairs[0].Item1.transform.position.x < 0) {
                        var stalePair = goamPairs[0];
                        goamPairs.Remove(stalePair);
                        GameObject.Destroy(stalePair.Item1);
                        GameObject.Destroy(stalePair.Item2);
                    }
                }
                if(pd.health < pd.maxHealthBase) {
                    endGame();
                    break;
                }
                await Task.Yield();
            }
        }

        private GameObject spawnGoam(bool isTop, double height, float offset) {
            float spawnX = 70;
            GameObject output = GameObject.Instantiate(pickGoam(isTop), new Vector3(spawnX, (float)height + (isTop ? 1 : -1) * offset), pickGoam(isTop).transform.rotation);
            output.GetComponent<tk2dSpriteAnimator>().enabled = false;
            output.transform.SetScaleY(2);
            output.SetActiveChildren(false);
            return output;
        }

        private GameObject pickGoam(bool isTop) {
            return isTop ? goamHighPrefab : goamLowPrefab;
        }

        private async void endGame() {
            int saveSlot = GameManager.instance.profileID;
            HeroController.instance.IgnoreInputWithoutReset();
            HeroController.instance.cState.invulnerable = true;
            Rigidbody2D rb = HeroController.instance.GetComponent<Rigidbody2D>();
            rb.isKinematic = true;
            float startTime = Time.time;
            while(Time.time - startTime < 3.5f) {
                rb.velocity = Vector2.zero;
                await Task.Yield();
            }
            GameManager.instance.StartCoroutine(GameManager.instance.ReturnToMainMenu(GameManager.ReturnToMainMenuSaveModes.DontSave, null));
        }
    }

    public class SendGoamStart: FsmStateAction {
        public override void OnEnter() {
            FlappyKnight.instance.runGoamSpawning();
            Finish();
        }
    }
}