using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using XD.Weather;

/// <summary>
/// 专门用于控制从晴天过渡到雨天的天气控制器
/// 提供简单的GUI界面和自动化的10秒过渡功能
/// </summary>
public class WeatherTransitionController : MonoBehaviour
{
    [Header("渲染管线配置")]
    /// <summary>
    /// Universal渲染管线资源
    /// </summary>
    public UniversalRenderPipelineAsset urpAsset;

    [Header("天气配置")]
    /// <summary>
    /// 晴天天气配置ID（需要在WeatherGroup中设置对应的晴天配置）
    /// </summary>
    [SerializeField]
    public int sunnyWeatherId = 0;

    /// <summary>
    /// 雨天天气配置ID（需要在WeatherGroup中设置对应的雨天配置）
    /// </summary>
    [SerializeField]
    public int rainyWeatherId = 1;

    /// <summary>
    /// 过渡曲线ID（用于控制过渡效果）
    /// </summary>
    [SerializeField]
    public int transitionCurveId = 0;

    [Header("过渡设置")]
    /// <summary>
    /// 过渡持续时间（秒）
    /// </summary>
    [SerializeField]
    public float transitionDuration = 10f;

    /// <summary>
    /// 是否在开始时自动开始过渡
    /// </summary>
    [SerializeField]
    public bool autoStartTransition = false;

    /// <summary>
    /// 自动开始过渡的延迟时间（秒）
    /// </summary>
    [SerializeField]
    public float autoStartDelay = 2f;

    // 私有变量
    private TimeOfDay timeOfDay;
    private WeatherGroup weatherGroup;
    private bool isTransitioning = false;
    private float transitionTimer = 0f;
    private bool isShowGUI = true;

    // GUI样式
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle progressBarStyle;

    /// <summary>
    /// Unity Awake回调，初始化组件和设置
    /// </summary>
    private void Awake()
    {
        // 激活URP渲染管线
        if (urpAsset != null)
        {
            GraphicsSettings.renderPipelineAsset = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
        }

        // 获取TimeOfDay组件
        timeOfDay = GetComponent<TimeOfDay>();
        if (timeOfDay == null)
        {
            Debug.LogError("WeatherTransitionController: 未找到TimeOfDay组件！");
            return;
        }

        // 设置当前时间
        timeOfDay.SetDate(System.DateTime.Now);
        timeOfDay.updateMode = UpdateMode.Client;

        // 获取天气组
        weatherGroup = timeOfDay.weatherGroup;
        if (weatherGroup == null)
        {
            Debug.LogError("WeatherTransitionController: TimeOfDay组件未设置WeatherGroup！");
            return;
        }

        // 初始化天气字典
        weatherGroup.InitWeatherDic();

        // 初始化GUI样式
        InitializeGUIStyles();

        Debug.Log("WeatherTransitionController: 初始化完成");
    }

    /// <summary>
    /// Unity Start回调，设置初始天气状态
    /// </summary>
    void Start()
    {
        // 设置为晴天作为初始状态
        if (weatherGroup.IsWeatherProfileExist(sunnyWeatherId))
        {
            timeOfDay.OnWeatherUpdate(sunnyWeatherId);
            Debug.Log($"WeatherTransitionController: 设置初始天气为晴天 (ID: {sunnyWeatherId})");
        }
        else
        {
            Debug.LogError($"WeatherTransitionController: 晴天配置ID {sunnyWeatherId} 不存在！");
        }

        // 如果设置了自动开始过渡，启动协程
        if (autoStartTransition)
        {
            StartCoroutine(AutoStartTransitionCoroutine());
        }
    }

    /// <summary>
    /// Unity Update回调，处理过渡逻辑
    /// </summary>
    void Update()
    {
        if (weatherGroup == null || timeOfDay == null)
            return;

        // 处理天气过渡
        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            
            // 计算标准化的过渡进度 (0到1)
            float normalizedProgress = Mathf.Clamp01(transitionTimer / transitionDuration);
            
            // 更新天气过渡
            int result = timeOfDay.OnWeatherTransitioUpdate(sunnyWeatherId, rainyWeatherId, transitionCurveId, normalizedProgress);
            
            if (result != 0)
            {
                Debug.LogError($"WeatherTransitionController: 天气过渡更新失败，错误代码: {result}");
                StopTransition();
                return;
            }

            // 检查过渡是否完成
            if (transitionTimer >= transitionDuration)
            {
                CompleteTransition();
            }
        }
        else
        {
            // 如果不在过渡中，正常更新当前天气
            timeOfDay.OnWeatherUpdate(sunnyWeatherId);
        }
    }

    /// <summary>
    /// 开始从晴天到雨天的过渡
    /// </summary>
    public void StartTransition()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("WeatherTransitionController: 过渡已在进行中");
            return;
        }

        // 验证天气配置是否存在
        if (!weatherGroup.IsWeatherProfileExist(sunnyWeatherId))
        {
            Debug.LogError($"WeatherTransitionController: 晴天配置ID {sunnyWeatherId} 不存在！");
            return;
        }

        if (!weatherGroup.IsWeatherProfileExist(rainyWeatherId))
        {
            Debug.LogError($"WeatherTransitionController: 雨天配置ID {rainyWeatherId} 不存在！");
            return;
        }

        if (!weatherGroup.IsPropertyCurveExist(transitionCurveId))
        {
            Debug.LogError($"WeatherTransitionController: 过渡曲线ID {transitionCurveId} 不存在！");
            return;
        }

        // 开始过渡
        isTransitioning = true;
        transitionTimer = 0f;
        
        Debug.Log($"WeatherTransitionController: 开始从晴天过渡到雨天，持续时间: {transitionDuration}秒");
    }

    /// <summary>
    /// 停止当前过渡
    /// </summary>
    public void StopTransition()
    {
        if (!isTransitioning)
            return;

        isTransitioning = false;
        transitionTimer = 0f;
        
        Debug.Log("WeatherTransitionController: 过渡已停止");
    }

    /// <summary>
    /// 重置到晴天状态
    /// </summary>
    public void ResetToSunny()
    {
        StopTransition();
        
        if (weatherGroup.IsWeatherProfileExist(sunnyWeatherId))
        {
            timeOfDay.OnWeatherUpdate(sunnyWeatherId);
            Debug.Log("WeatherTransitionController: 重置为晴天");
        }
    }

    /// <summary>
    /// 直接切换到雨天状态
    /// </summary>
    public void SetToRainy()
    {
        StopTransition();
        
        if (weatherGroup.IsWeatherProfileExist(rainyWeatherId))
        {
            timeOfDay.OnWeatherUpdate(rainyWeatherId);
            Debug.Log("WeatherTransitionController: 直接切换到雨天");
        }
    }

    /// <summary>
    /// 完成过渡
    /// </summary>
    private void CompleteTransition()
    {
        isTransitioning = false;
        transitionTimer = transitionDuration;
        
        // 确保最终状态是雨天
        timeOfDay.OnWeatherUpdate(rainyWeatherId);
        
        Debug.Log("WeatherTransitionController: 天气过渡完成，当前为雨天");
    }

    /// <summary>
    /// 自动开始过渡的协程
    /// </summary>
    private IEnumerator AutoStartTransitionCoroutine()
    {
        yield return new WaitForSeconds(autoStartDelay);
        StartTransition();
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeGUIStyles()
    {
        labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;

        buttonStyle = new GUIStyle();
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.fontSize = 12;
        buttonStyle.normal.background = Texture2D.whiteTexture;

        progressBarStyle = new GUIStyle();
        progressBarStyle.normal.textColor = Color.white;
        progressBarStyle.fontSize = 12;
    }

    /// <summary>
    /// Unity OnGUI回调，绘制控制面板
    /// </summary>
    private void OnGUI()
    {
        if (weatherGroup == null || timeOfDay == null)
            return;

        // 设置GUI区域
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        
        // 标题
        GUILayout.Label("天气过渡控制器", labelStyle);
        GUILayout.Space(10);

        // 显示当前状态
        string currentStatus = isTransitioning ? 
            $"过渡中... ({transitionTimer:F1}s / {transitionDuration}s)" : 
            "晴天状态";
        GUILayout.Label($"当前状态: {currentStatus}", labelStyle);
        
        // 进度条（仅在过渡时显示）
        if (isTransitioning)
        {
            float progress = transitionTimer / transitionDuration;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"过渡进度: {progress * 100:F0}%", labelStyle);
            GUILayout.EndHorizontal();
            
            // 简单的文本进度条
            Rect progressRect = GUILayoutUtility.GetRect(250, 20);
            GUI.Box(progressRect, "");
            Rect fillRect = new Rect(progressRect.x, progressRect.y, progressRect.width * progress, progressRect.height);
            GUI.Box(fillRect, "", GUI.skin.button);
        }

        GUILayout.Space(10);

        // 控制按钮
        GUI.enabled = !isTransitioning;
        if (GUILayout.Button("开始过渡到雨天"))
        {
            StartTransition();
        }

        if (GUILayout.Button("重置为晴天"))
        {
            ResetToSunny();
        }

        if (GUILayout.Button("直接切换到雨天"))
        {
            SetToRainy();
        }

        GUI.enabled = isTransitioning;
        if (GUILayout.Button("停止过渡"))
        {
            StopTransition();
        }

        GUI.enabled = true;

        GUILayout.Space(10);

        // 配置设置
        GUILayout.Label("配置设置:", labelStyle);
        
        // 过渡时间滑块
        GUILayout.BeginHorizontal();
        GUILayout.Label($"过渡时间: {transitionDuration:F1}s", GUILayout.Width(120));
        if (!isTransitioning)
        {
            transitionDuration = GUILayout.HorizontalSlider(transitionDuration, 1f, 30f);
        }
        GUILayout.EndHorizontal();

        // 显示天气ID配置
        GUILayout.Label($"晴天ID: {sunnyWeatherId}, 雨天ID: {rainyWeatherId}", labelStyle);

        GUILayout.EndArea();
    }

    /// <summary>
    /// 获取当前过渡进度 (0-1)
    /// </summary>
    public float GetTransitionProgress()
    {
        if (!isTransitioning)
            return 0f;
        
        return Mathf.Clamp01(transitionTimer / transitionDuration);
    }

    /// <summary>
    /// 检查是否正在过渡
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// 设置过渡时间
    /// </summary>
    /// <param name="duration">过渡持续时间（秒）</param>
    public void SetTransitionDuration(float duration)
    {
        if (duration > 0f)
        {
            transitionDuration = duration;
            Debug.Log($"WeatherTransitionController: 过渡时间设置为 {duration} 秒");
        }
    }

    /// <summary>
    /// 设置天气配置ID
    /// </summary>
    /// <param name="sunnyId">晴天配置ID</param>
    /// <param name="rainyId">雨天配置ID</param>
    public void SetWeatherIds(int sunnyId, int rainyId)
    {
        sunnyWeatherId = sunnyId;
        rainyWeatherId = rainyId;
        Debug.Log($"WeatherTransitionController: 天气ID设置为 晴天:{sunnyId}, 雨天:{rainyId}");
    }

    /// <summary>
    /// 设置过渡曲线ID
    /// </summary>
    /// <param name="curveId">曲线ID</param>
    public void SetTransitionCurveId(int curveId)
    {
        transitionCurveId = curveId;
        Debug.Log($"WeatherTransitionController: 过渡曲线ID设置为 {curveId}");
    }

    /// <summary>
    /// 在Inspector中验证配置
    /// </summary>
    private void OnValidate()
    {
        if (transitionDuration <= 0f)
            transitionDuration = 10f;
    }

    /// <summary>
    /// 在编辑器中显示帮助信息
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示状态信息
        if (isTransitioning)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one);
        }
    }
}
