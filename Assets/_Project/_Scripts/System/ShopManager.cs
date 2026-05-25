using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; 

public class ShopManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainShopParent;   // 1. 전체 상점 UI
    public GameObject selectionPanel;   // 2. 카테고리 선택 창 (타워 살래? 거점 강화할래?)
    public GameObject towerShopPanel;   // 3. 타워 목록 창
    public GameObject BaseShopPanel; // 🌟 4. 새로 추가된 거점 강화 창!
    
    public GameObject confirmPanel;     // 5. 구매 확인 창
    public TextMeshProUGUI confirmText; 
    public GameObject placeConfirmPanel; // 6. 맵 설치 확인 창

    [Header("Managers & References")]
    public GridManager gridManager;
    public BaseBuilding baseBuilding;   // 🌟 거점 스크립트 연결용!

    private bool isShopOpen = false;
    private bool isPlacingMode = false;
    private GameObject selectedTowerPrefab;
    private Vector3 pendingPlacePosition;
    private Vector3 currentGridPosition;

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame && !isPlacingMode)
        {
            ToggleShop();
        }

        if (isPlacingMode)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            bool isValidPos = gridManager.TryGetGridPosition(mousePos, out currentGridPosition);
          
            if (isValidPos)
            {
                bool isOccupied = gridManager.IsPositionOccupied(currentGridPosition);
                gridManager.ShowHoverIndicator(currentGridPosition, true, !isOccupied);

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (isOccupied)
                    {
                        Debug.LogWarning("이미 타워가 설치된 자리입니다!");
                    }
                    else
                    {
                        pendingPlacePosition = currentGridPosition; 
                        AskPlaceConfirm();                    
                    }
                }
            }
            else
            {
                gridManager.ShowHoverIndicator(Vector3.zero, false);
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }
    }

    private void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        mainShopParent.SetActive(isShopOpen);
        
        if (isShopOpen) 
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // 🌟 상점을 처음 열 때는 무조건 '선택 창'만 띄우고 나머지는 다 끕니다.
            selectionPanel.SetActive(true);
            towerShopPanel.SetActive(false);
            if (BaseShopPanel != null) BaseShopPanel.SetActive(false); 
            if (confirmPanel != null) confirmPanel.SetActive(false); 
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // ==============================================================
    // 🌟 패널 이동 (Navigation) 로직
    // ==============================================================

    public void OpenTowerShopPanel()
    {
        selectionPanel.SetActive(false); 
        if (BaseShopPanel != null) BaseShopPanel.SetActive(false);
        towerShopPanel.SetActive(true);  
    }

    // 🌟 거점 강화 패널 열기 버튼에 연결할 함수
    public void OpenBaseShopPanel()
    {
        selectionPanel.SetActive(false);
        towerShopPanel.SetActive(false);
        if (BaseShopPanel != null) BaseShopPanel.SetActive(true);
    }

    // 뒤로 가기 버튼에 연결할 함수 (타워 목록이나 강화 창에서 선택 창으로 돌아갈 때)
    public void BackToSelection()
    {
        towerShopPanel.SetActive(false);
        if (BaseShopPanel != null) BaseShopPanel.SetActive(false);
        selectionPanel.SetActive(true);
    }

    // ==============================================================
    // 🌟 거점 강화 구매 로직
    // ==============================================================

    public void BuyMaxHealthUpgrade()
    {
        if (baseBuilding != null)
        {
            // TODO: 재화(골드) 차감 로직을 여기에 넣으세요!
            // 예: if(playerMoney >= 100) { playerMoney -= 100; ... }
            baseBuilding.UpgradeMaxHealth();
        }
    }

    public void BuyDefenseUpgrade()
    {
        if (baseBuilding != null)
        {
            // TODO: 재화 차감 로직 추가
            baseBuilding.UpgradeDefense();
        }
    }

    // ==============================================================
    // 이하 타워 설치 관련 로직 (기존과 동일)
    // ==============================================================

    public void BuyTower(GameObject towerPrefab)
    {
        selectedTowerPrefab = towerPrefab; 
        towerShopPanel.SetActive(false);   
        confirmPanel.SetActive(true);      

        string towerNameKOR = towerPrefab.name switch
        {
            "ArcherTower" => "아처 타워",
            "WaterTower" => "서리 타워",
            "LightningTower" => "라이트닝 타워",
            "PoisonTower" => "포이즌 타워",
            "WindTower" => "윈드 타워",
            _ => towerPrefab.name
        };

        if (confirmText != null)
        {
            confirmText.text = $"[{towerNameKOR}]를 선택하셨습니다.\n해당 타워를 설치하시겠습니까?";
        }
    }

    public void ConfirmBuy()
    {
        confirmPanel.SetActive(false);
        mainShopParent.SetActive(false); 
        isShopOpen = false;
        
        gridManager.ShowGrid(true); 
        isPlacingMode = true; 
    }

    public void CancelBuy()
    {
        selectedTowerPrefab = null; 
        confirmPanel.SetActive(false);   
        towerShopPanel.SetActive(true);  
    }

    private void AskPlaceConfirm()
    {
        isPlacingMode = false; 
        gridManager.ShowHoverIndicator(Vector3.zero, false); 
        placeConfirmPanel.SetActive(true); 
    }

    public void ConfirmPlacement()
    {
        if (selectedTowerPrefab != null)
        {
            Instantiate(selectedTowerPrefab, pendingPlacePosition, Quaternion.identity);
            gridManager.MarkPositionOccupied(pendingPlacePosition);
        }

        isPlacingMode = false;
        if (placeConfirmPanel != null) placeConfirmPanel.SetActive(false);
        
        if (gridManager != null)
        {
            gridManager.ShowHoverIndicator(Vector3.zero, false);
            gridManager.ShowGrid(false);
        }

        if (mainShopParent != null)
        {
            mainShopParent.SetActive(true);
            
            // 설치 완료 후 다시 '타워 상점 창'으로 돌아감
            if (selectionPanel != null) selectionPanel.SetActive(false); 
            if (towerShopPanel != null) towerShopPanel.SetActive(true);

            RectTransform rt = mainShopParent.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero; 
                rt.localScale = Vector3.one;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void CancelPlacementConfirm()
    {
        isPlacingMode = false;
        gridManager.ShowHoverIndicator(Vector3.zero, false);
        gridManager.ShowGrid(false);
        placeConfirmPanel.SetActive(false);

        if (mainShopParent != null)
        {
            mainShopParent.SetActive(true);
            
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (towerShopPanel != null) towerShopPanel.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void CancelPlacement()
    {
        isPlacingMode = false;
        gridManager.ShowGrid(false);
        gridManager.ShowHoverIndicator(Vector3.zero, false);
        selectedTowerPrefab = null;
    }
}