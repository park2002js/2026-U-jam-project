using UnityEngine;

namespace Ujam.Runtime.Item
{
    /// <summary>
    /// 아이템별 동작을 담는 일반 C# 클래스. 효과 하나당 클래스 하나를 만들고 Catalog에 등록한다.
    /// 생성자에는 조정 수치만 받는다. 보유자/카운터/적중 목록을 필드에 저장하지 않는다.
    /// Effect의 설정은 공유하므로 카운터는 context.Player, 작업 수명은 context.Item.Runtime에 맡긴다.
    /// </summary>
    public abstract class ItemEffect
    {
        /// <summary>
        /// 이 아이템이 원하는 순간에 재생할 효과음 원본들. 재생 상태나 AudioSource가 아니라 공유 AudioClip 참조를 보관한다.
        /// 실제 폴더는 Assets/_Project/Resources/Audio/Item/아이템ID/이며, ID는 ItemCatalog의 숫자 문자열(예: 화염구는 2)이다.
        /// 예: Audio/Item/2/FireballCast.wav, Audio/Item/2/FireballImpact.wav. 한 폴더에 클립을 한 개 또는 여러 개 넣을 수 있다.
        /// 각 구체 Effect 생성자의 base("2")가 해당 폴더를 한 번 읽는다. Item 복사본은 Effect와 이 목록을 함께 공유한다.
        /// 파일명은 고정 규격이 아니다. 클립 묶음을 정리할 때 아이템별 폴더에 배치하고, 그 의미와 파일명을 해당 Effect에서 연결한다.
        /// GetClip("FireballImpact")처럼 확장자를 제외한 실제 파일명으로 조회한다. 배열 순서에는 의존하지 않는다.
        /// 폴더가 없거나 비었으면 clips는 null이며, 이름이 없을 때도 GetClip은 null을 반환한다.
        /// 실제 발동 조건을 통과한 지점에서 SFXManager.Instance.Play(GetClip("FireballCast"))를 호출하거나,
        /// 실제 적중/폭발 지점에서 SFXManager.Instance.PlayAt(GetClip("FireballImpact"), position)을 호출한다.
        /// null도 Manager에 전달하면 호출 파일/함수/줄을 기록하고 재생을 생략한다. clips[index]로 직접 접근하면 null 처리를 우회하므로 GetClip을 사용한다.
        /// 파일을 추가하는 것만으로 자동 재생되지는 않는다. 재생 시점과 파일명 선택은 각 Effect의 책임이다.
        /// </summary>
        protected readonly AudioClip[] clips;

        /// <summary>아이템 정의를 만들 때 효과음을 읽는다. 경로나 클립이 없으면 null을 유지한다.</summary>
        protected ItemEffect(string itemId = null)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;

            var loaded = Resources.LoadAll<AudioClip>($"Audio/Item/{itemId}");
            clips = loaded.Length > 0 ? loaded : null;
        }

        /// <summary>각 Effect가 알고 있는 파일명으로 조회한다. 미준비 효과음은 SFXManager에 null로 전달한다.</summary>
        protected AudioClip GetClip(string name)
        {
            if (clips == null) return null;

            foreach (var clip in clips)
                if (clip != null && clip.name == name) return clip;

            return null;
        }

        /// <summary>이벤트를 받은 순간의 조건을 검사한다. 기본값은 항상 발동이며 조건은 최대 두 개로 유지한다.</summary>
        public virtual bool CanExecute(ItemUseContext context) => true;
        /// <summary>해당 아이템의 동작을 구현한다. 지연/지속 동작은 context.Run(코루틴)으로 실행한다.</summary>
        public abstract void Execute(ItemUseContext context);
        /// <summary>장착 시 추가 준비가 필요할 때만 재정의한다. 이벤트 구독은 Runtime이 담당한다.</summary>
        public virtual void OnEquip(ItemUseContext context) { }
        /// <summary>장착 해제 시 효과 고유의 정리가 필요한 경우만 재정의한다. 버프/오브젝트는 공통으로 정리된다.</summary>
        public virtual void OnUnequip(ItemUseContext context) { }
    }
}
