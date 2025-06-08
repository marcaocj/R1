using UnityEngine;
using System.Collections.Generic;

// ================================
// CameraController.cs - Unity 6 Corrigido
// ================================
public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 5, -10);
    public float followSpeed = 2f;
    public float rotationSpeed = 2f;
    
    [Header("Camera Limits")]
    public float minDistance = 3f;
    public float maxDistance = 15f;
    public float minHeight = 1f;
    public float maxHeight = 10f;
    
    [Header("Smooth Movement")]
    public bool useSmoothDamp = true;
    public bool lookAtTarget = true;
    
    private Vector3 currentVelocity;
    private Camera cameraComponent;
    
    private void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("CameraController: Camera component não encontrado!");
        }
    }
    
    private void Start()
    {
        if (target == null)
        {
            // Tentar encontrar o player automaticamente
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                Debug.Log("CameraController: Player encontrado automaticamente");
            }
            else
            {
                Debug.LogWarning("CameraController: Nenhum target encontrado!");
            }
        }
        
        // Posicionar câmera inicialmente
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) 
        {
            // Tentar encontrar target novamente
            FindTarget();
            return;
        }
        
        UpdateCameraPosition();
        UpdateCameraRotation();
    }
    
    private void UpdateCameraPosition()
    {
        Vector3 desiredPosition = target.position + offset;
        
        // Aplicar limites de distância
        desiredPosition = ApplyDistanceLimits(desiredPosition);
        
        if (useSmoothDamp)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, 
                desiredPosition, 
                ref currentVelocity, 
                1f / followSpeed
            );
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position, 
                desiredPosition, 
                followSpeed * Time.deltaTime
            );
        }
    }
    
    private void UpdateCameraRotation()
    {
        if (lookAtTarget && target != null)
        {
            // Smooth rotation towards target
            Vector3 direction = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
        }
    }
    
    private Vector3 ApplyDistanceLimits(Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - target.position;
        float distance = direction.magnitude;
        
        // Aplicar limites de distância
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        
        // Aplicar limites de altura
        Vector3 result = target.position + direction.normalized * distance;
        result.y = Mathf.Clamp(result.y, target.position.y + minHeight, target.position.y + maxHeight);
        
        return result;
    }
    
    private void FindTarget()
    {
        if (target != null) return;
        
        // Tentar encontrar pelo GameManager
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            target = GameManager.Instance.CurrentPlayer.transform;
            Debug.Log("CameraController: Target encontrado via GameManager");
            return;
        }
        
        // Tentar encontrar por tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            Debug.Log("CameraController: Target encontrado por tag");
        }
    }
    
    /// <summary>
    /// Define um novo alvo para a câmera
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        currentVelocity = Vector3.zero; // Reset velocity for smooth transition
        
        if (target != null)
        {
            Debug.Log($"CameraController: Novo target definido: {target.name}");
        }
    }
    
    /// <summary>
    /// Define um novo offset para a câmera
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    /// <summary>
    /// Teleporta a câmera para a posição do target imediatamente
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            currentVelocity = Vector3.zero;
            
            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }
    
    /// <summary>
    /// Configurações de distância dinâmica
    /// </summary>
    public void SetDistanceLimits(float min, float max)
    {
        minDistance = Mathf.Max(0.1f, min);
        maxDistance = Mathf.Max(minDistance + 0.1f, max);
    }
    
    /// <summary>
    /// Configurações de altura dinâmica
    /// </summary>
    public void SetHeightLimits(float min, float max)
    {
        minHeight = min;
        maxHeight = Mathf.Max(minHeight + 0.1f, max);
    }
    
    // Propriedades públicas
    public bool HasTarget => target != null;
    public float CurrentDistance => target != null ? Vector3.Distance(transform.position, target.position) : 0f;
    
    private void OnValidate()
    {
        // Validar limites no editor
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        maxHeight = Mathf.Max(minHeight + 0.1f, maxHeight);
        followSpeed = Mathf.Max(0.1f, followSpeed);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Desenhar limites de distância
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, minDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, maxDistance);
        
        // Desenhar offset
        Gizmos.color = Color.blue;
        Vector3 targetPos = target.position + offset;
        Gizmos.DrawWireCube(targetPos, Vector3.one * 0.5f);
        Gizmos.DrawLine(target.position, targetPos);
        
        // Desenhar limites de altura
        Gizmos.color = Color.green;
        Vector3 minHeightPos = target.position + Vector3.up * minHeight;
        Vector3 maxHeightPos = target.position + Vector3.up * maxHeight;
        Gizmos.DrawLine(minHeightPos + Vector3.left, minHeightPos + Vector3.right);
        Gizmos.DrawLine(maxHeightPos + Vector3.left, maxHeightPos + Vector3.right);
    }
}