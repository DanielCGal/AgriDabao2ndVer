using UnityEngine;

namespace AgriDabao3D
{
    public class TreeBillboardLOD : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Distance Settings")]
        public float switchToBillboardDistance = 70f;
        public float switchBackToModelDistance = 55f;
        public float updateInterval = 0.25f;

        [Header("3D Model Root")]
        public GameObject modelRoot;

        [Header("Billboard Sprite")]
        public Sprite billboardSprite;
        public Vector2 billboardSize = new Vector2(8f, 10f);
        public float billboardHeightOffset = 5f;

        private GameObject billboardObject;
        private SpriteRenderer billboardRenderer;
        private bool usingBillboard;
        private float timer;

        private void Start()
        {
            if (player == null)
            {
                FirstPersonTerrainController controller =
                    Object.FindFirstObjectByType<FirstPersonTerrainController>();

                if (controller != null)
                    player = controller.transform;
            }

            if (modelRoot == null)
                modelRoot = gameObject;

            CreateBillboard();
            SetBillboardMode(false);
        }

        private void Update()
        {
            if (player == null)
                return;

            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;
                CheckDistance();
            }

            if (usingBillboard)
                FacePlayer();
        }

        private void CreateBillboard()
        {
            if (billboardSprite == null)
                return;

            billboardObject = new GameObject("Tree_Billboard_LOD");
            billboardObject.transform.SetParent(transform, false);
            billboardObject.transform.localPosition = new Vector3(0f, billboardHeightOffset, 0f);

            billboardRenderer = billboardObject.AddComponent<SpriteRenderer>();
            billboardRenderer.sprite = billboardSprite;
            billboardRenderer.sortingOrder = 10;

            billboardObject.transform.localScale = new Vector3(
                billboardSize.x,
                billboardSize.y,
                1f
            );
        }

        private void CheckDistance()
        {
            float distance = Vector3.Distance(player.position, transform.position);

            if (!usingBillboard && distance >= switchToBillboardDistance)
            {
                SetBillboardMode(true);
            }
            else if (usingBillboard && distance <= switchBackToModelDistance)
            {
                SetBillboardMode(false);
            }
        }

        private void SetBillboardMode(bool useBillboard)
        {
            usingBillboard = useBillboard;

            if (modelRoot != null)
                modelRoot.SetActive(!useBillboard);

            if (billboardObject != null)
                billboardObject.SetActive(useBillboard);
        }

        private void FacePlayer()
        {
            if (billboardObject == null || player == null)
                return;

            Vector3 direction = player.position - billboardObject.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            billboardObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
