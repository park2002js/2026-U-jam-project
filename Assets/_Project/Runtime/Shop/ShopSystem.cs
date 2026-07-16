using System;
using System.Collections.Generic;
using UJam.Runtime.Phase;

namespace UJam.Runtime.Shop
{
    public sealed class ShopSystem
    {
        // 상점용 상품 카탈로그
        private readonly ShopCatalog _catalog;

        // 상점용 지갑
        private readonly Wallet _wallet;

        // 처리 완료 유효 요청 식별자
        private readonly HashSet<string> _processedRequestIds;

        // 상점 의존성 명시 저장
        public ShopSystem(ShopCatalog catalog, Wallet wallet)
        {
            // 상품 카탈로그 의존성 저장
            _catalog = catalog;

            // 지갑 의존성 저장
            _wallet = wallet;

            // 새 처리 기록 생성
            _processedRequestIds = new HashSet<string>(StringComparer.Ordinal);
        }

        // 구매 요청 검증과 성공 주문 생성
        public PurchaseResult Purchase(PurchaseRequest request)
        {
            // 요청 구조 우선 검증
            if (!IsStructurallyValid(request))
            {
                // 구조 오류 요청 식별자 미소비
                return PurchaseResult.Failed(PurchaseFailureReason.InvalidRequest);
            }

            // 다른 결과보다 앞선 중복 요청 차단
            if (!_processedRequestIds.Add(request.PurchaseRequestId))
            {
                // 중복 요청 지갑 변경 없음
                return PurchaseResult.Failed(PurchaseFailureReason.DuplicateRequest);
            }

            // Preparation 외 단계 구매 차단
            if (request.Phase != PhaseState.Preparation)
            {
                // 유효 요청 식별자 소비 상태
                return PurchaseResult.Failed(PurchaseFailureReason.InvalidPhase);
            }

            // 카탈로그 또는 지갑 부재 의존성 오류
            if (_catalog == null || _wallet == null)
            {
                // 의존성 오류 지갑 변경 없음
                return PurchaseResult.Failed(PurchaseFailureReason.InvalidRequest);
            }

            // 카탈로그 요청 상품 조회
            if (!_catalog.TryGet(request.ProductId, out ShopProduct product))
            {
                // 상품 부재 지갑 변경 없음
                return PurchaseResult.Failed(PurchaseFailureReason.ProductNotFound);
            }

            // 상품 가격 단일 차감
            if (!_wallet.TrySpend(product.Price))
            {
                // 잔액 부족 지갑 상태 유지
                return PurchaseResult.Failed(PurchaseFailureReason.InsufficientFunds);
            }

            // 요청 식별자 주문 식별자 사용
            PurchaseOrder order = PurchaseOrder.Create(request.PurchaseRequestId, product.ProductId);

            // 차감 가격과 주문 성공 결과 반환
            return PurchaseResult.Succeeded(order, product.Price);
        }

        // 기본 요청과 미정의 Phase 구조 오류 판정
        private static bool IsStructurallyValid(PurchaseRequest request)
        {
            // 상품 식별자 유효성 확인
            if (!request.ProductId.IsValid)
            {
                // 잘못된 상품 식별자 차단
                return false;
            }

            // 요청 식별자 공백 여부 확인
            if (string.IsNullOrWhiteSpace(request.PurchaseRequestId))
            {
                // 잘못된 요청 식별자 차단
                return false;
            }

            // 공개 계약 Phase 정의 여부 확인
            if (!IsDefinedPhase(request.Phase))
            {
                // 미정의 Phase 차단
                return false;
            }

            // 구조 검증 통과 반환
            return true;
        }

        // Phase 열거형 정의값 확인
        private static bool IsDefinedPhase(PhaseState phase)
        {
            // 공개 Phase 세 값 확인
            if (phase == PhaseState.Preparation
                || phase == PhaseState.Combat
                || phase == PhaseState.StageClear)
            {
                // 정의 Phase 반환
                return true;
            }

            // 미정의 Phase 반환
            return false;
        }
    }
}
