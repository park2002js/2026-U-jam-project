using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace PolygonArsenal
{

public class PolygonSceneSelect : MonoBehaviour
{
	public bool GUIHide = false;
	public bool GUIHide2 = false;
	public bool GUIHide3 = false;
	public bool GUIHide4 = false;
	public bool GUIHide5 = false;
	
	//Combat Scenes
	
    public void CBLoadSceneMissiles()		{ LoadDemoScene("PolyMissiles");	}
	public void CBLoadSceneBeams()			{ LoadDemoScene("PolyBeams"); 		}
	public void CBLoadSceneBeams2()			{ LoadDemoScene("PolyBeams2"); 	}
	public void CBLoadSceneAura()			{ LoadDemoScene("PolyAura"); 		}
	public void CBLoadSceneAura2()			{ LoadDemoScene("PolyAura2");	 	}
	public void CBLoadSceneAura3()			{ LoadDemoScene("PolyAura3"); 		}
	public void CBLoadSceneAura4()			{ LoadDemoScene("PolyAura4"); 		}
	public void CBLoadSceneBarrage()		{ LoadDemoScene("PolyBarrage"); 	}
	public void CBLoadSceneBarrage2()		{ LoadDemoScene("PolyBarrage2"); 	}
	public void CBLoadSceneChains()			{ LoadDemoScene("PolyChains"); 	}
	public void CBLoadSceneChains2()		{ LoadDemoScene("PolyChains2"); 	}
	public void CBLoadSceneCleave()			{ LoadDemoScene("PolyCleave"); 	}
	public void CBLoadSceneCombat01()		{ LoadDemoScene("PolyCombat01"); 	}
	public void CBLoadSceneCombat02()		{ LoadDemoScene("PolyCombat02"); 	}
	public void CBLoadSceneCurses()			{ LoadDemoScene("PolyCurses"); 	}
	public void CBLoadSceneDeath()			{ LoadDemoScene("PolyDeath");	 	}
	public void CBLoadSceneEnchant()		{ LoadDemoScene("PolyEnchant"); 	}
	public void CBLoadSceneExploMini()		{ LoadDemoScene("PolyExploMini"); 	}
	public void CBLoadSceneGore()			{ LoadDemoScene("PolyGore"); 		}
	public void CBLoadSceneHitscan()		{ LoadDemoScene("PolyHitscan"); 	}
	public void CBLoadSceneNecromancy()		{ LoadDemoScene("PolyNecromancy");	}
	public void CBLoadSceneNova()			{ LoadDemoScene("PolyNova"); 		}
	public void CBLoadSceneOrbitalBeam()	{ LoadDemoScene("PolyOrbitalBeam");}
	public void CBLoadSceneSpikes()			{ LoadDemoScene("PolySpikes"); 	}
	public void CBLoadSceneSpikes2()		{ LoadDemoScene("PolySpikes2"); 	}
	public void CBLoadSceneSpikes3()		{ LoadDemoScene("PolySpikes3"); 	}
	public void CBLoadSceneSpikes4()		{ LoadDemoScene("PolySpikes4"); 	}
	public void CBLoadSceneSurfaceDmg()		{ LoadDemoScene("PolySurfaceDmg");	}
	public void CBLoadSceneSword()			{ LoadDemoScene("PolySword"); 		}
	public void CBLoadSceneSwordTrail()		{ LoadDemoScene("PolySwordTrail");	}
	
	//Environment Scenes
	
	public void ENVLoadSceneConfetti()		{ LoadDemoScene("PolyConfetti"); 	}
	public void ENVLoadSceneEnvironment()	{ LoadDemoScene("PolyEnvironment");}
	public void ENVLoadSceneFire()			{ LoadDemoScene("PolyFire"); 		}
	public void ENVLoadSceneFire2()			{ LoadDemoScene("PolyFire2"); 		}
	public void ENVLoadSceneFireflies()		{ LoadDemoScene("PolyFireflies"); 	}
	public void ENVLoadSceneFireworks()		{ LoadDemoScene("PolyFireworks"); 	}
	public void ENVLoadSceneLiquid()		{ LoadDemoScene("PolyLiquid"); 	}
	public void ENVLoadSceneLiquid2()		{ LoadDemoScene("PolyLiquid2"); 	}
	public void ENVLoadSceneRocks()			{ LoadDemoScene("PolyRocks"); 		}
	public void ENVLoadSceneSparks()		{ LoadDemoScene("PolySparks"); 	}
	public void ENVLoadSceneTornado()		{ LoadDemoScene("PolyTornado"); 	}
	public void ENVLoadSceneWeather()		{ LoadDemoScene("PolyWeather"); 	}
	
	//Interactive Scenes
	
	public void INTLoadSceneBeamUp()		{ LoadDemoScene("PolyBeamUp"); 	}
	public void INTLoadSceneBlackHole()		{ LoadDemoScene("PolyBlackHole"); 	}
	public void INTLoadSceneHeal()			{ LoadDemoScene("PolyHeal"); 		}
	public void INTLoadSceneJets()			{ LoadDemoScene("PolyJets"); 		}
	public void INTLoadSceneLoot()			{ LoadDemoScene("PolyLoot"); 		}
	public void INTLoadScenePortal()		{ LoadDemoScene("PolyPortal"); 	}
	public void INTLoadScenePortal2()		{ LoadDemoScene("PolyPortal2"); 	}
	public void INTLoadScenePowerupIcon()	{ LoadDemoScene("PolyPowerupIcon");}
	public void INTLoadSceneSpawn()			{ LoadDemoScene("PolySpawn"); 		}
	public void INTLoadSceneTrails()		{ LoadDemoScene("PolyTrails"); 	}
	public void INTLoadSceneTreasure()		{ LoadDemoScene("PolyTreasure"); 	}
	public void INTLoadSceneTreasure2()		{ LoadDemoScene("PolyTreasure2"); 	}
	public void INTLoadSceneZones()			{ LoadDemoScene("PolyZones"); 		}
	
    static void LoadDemoScene(string sceneName)
    {
#if UNITY_EDITOR
        // Preview demos independently of the game's build scene list.
        foreach (string guid in AssetDatabase.FindAssets(sceneName + " t:Scene", new[] { "Assets/Polygon Arsenal/Demo/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != sceneName) continue;
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(sceneName);
    }

    static void ToggleCanvas(string name, ref bool hidden)
    {
        var target = GameObject.Find(name);
        if (target == null || !target.TryGetComponent<Canvas>(out var canvas)) return;
        canvas.enabled = !canvas.enabled;
        hidden = !canvas.enabled;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L)) ToggleCanvas("CanvasSceneSelectCom", ref GUIHide);
        if (Input.GetKeyDown(KeyCode.J)) ToggleCanvas("CanvasMissiles", ref GUIHide2);
        if (Input.GetKeyDown(KeyCode.H)) ToggleCanvas("CanvasTips", ref GUIHide3);
        if (Input.GetKeyDown(KeyCode.M)) ToggleCanvas("CanvasSceneSelectInt", ref GUIHide4);
        if (Input.GetKeyDown(KeyCode.N)) ToggleCanvas("CanvasSceneSelectEnv", ref GUIHide5);
    }
}

}
