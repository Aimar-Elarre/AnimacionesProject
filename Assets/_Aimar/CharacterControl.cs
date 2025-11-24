using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterControl : MonoBehaviour
{
    [Header("References")]
    public Animator animations;

    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 10f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 1.2f;

    [Header("Debug")]
    public bool enableLogs = true;

    private PlayerControls input;

    private Vector2 moveInput;
    private Vector3 playerVelocity;
    private bool isGrounded;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animations = GetComponent<Animator>();

        // Crear la instancia del asset generado
        input = new PlayerControls();
        if (enableLogs) Debug.Log("[Input] PlayerControls instanciado: " + (input != null));
    }

    void OnEnable()
    {
        if (input == null) input = new PlayerControls();
        // Habilitar action map
        try
        {
            input.Player.Enable();
            if (enableLogs) Debug.Log("[Input] input.Player habilitado");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Input] Error habilitando input.Player. ¿El action map 'Player' existe? " + e.Message);
        }

        // Suscribir solo al salto 
        input.Player.Jump.performed += ctx =>
        {
            if (enableLogs) Debug.Log("[Input] Jump.performed");
            TryJump();
        };
    }

    void OnDisable()
    {
        if (input != null)
        {
            try { input.Player.Disable(); }
            catch { }
        }
    }

    void FixedUpdate()
    {
        // Lectura por frame — esto es la forma fiable
        if (input == null)
        {
            if (enableLogs) Debug.LogWarning("[Input] input es null en Update, intentando reinstanciar.");
            input = new PlayerControls();
            input.Player.Enable();
        }

        // Leer Move cada frame
        moveInput = Vector2.zero;
        try
        {
            moveInput = input.Player.Move.ReadValue<Vector2>();
        }
        catch (System.Exception e)
        {
            if (enableLogs) Debug.LogError("[Input] Error leyendo Move: " + e.Message);
        }

        if (enableLogs)
        {
            // Imprime cuando hay input distinto de cero (o imprime siempre si quieres)
            if (moveInput.sqrMagnitude > 0.0001f)
                Debug.Log($"[Input] Move value: {moveInput}");
        }

        HandleMovement();
        GroundCheck();
        TryJump();
    }
    void GroundCheck()
    {
        Vector3 down = (Vector3.down).normalized;

        isGrounded = Physics.Raycast(transform.position, down, groundCheckDistance);
        if (isGrounded)
        {
            animations.SetBool("Jump", false);
        }
    }
    void HandleMovement()
    {
        Vector3 down = (Vector3.down).normalized;
        Vector3 up = -down;

        Vector3 camForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(Camera.main.transform.right, up).normalized;

        Vector3 moveDir = camForward * moveInput.y + camRight * moveInput.x;

        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            animations.SetBool("Move", true);
            Quaternion targetRot = Quaternion.LookRotation(moveDir, up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }
        else
        {
            animations.SetBool("Move", false);
        }
    }

    void TryJump()
    {
        {
            if (!isGrounded) return;
            if (input.Player.Jump.ReadValue<float>() > 0.5f)
            {
                Vector3 up = (Vector3.up).normalized;
                animations.SetBool("Jump", true);

                rb.AddForce(up * jumpForce, ForceMode.Impulse);
            }
        }
    }
}
