# WeatherTransitionController 使用说明

## 概述
`WeatherTransitionController` 是一个专门用于控制从晴天过渡到雨天的Unity组件。它基于现有的XD Weather系统，提供了简化的接口来实现10秒的天气过渡效果。

## 功能特性
- 🌞 自动从晴天过渡到雨天
- ⏱️ 可配置的过渡时间（默认10秒）
- 🎮 简单的GUI控制界面
- 🔄 支持手动控制和自动启动
- 📊 实时过渡进度显示

## 使用步骤

### 1. 设置组件
1. 将 `WeatherTransitionController.cs` 脚本添加到包含 `TimeOfDay` 组件的GameObject上
2. 在Inspector中配置以下参数：
   - **URP Asset**: 设置Universal渲染管线资源
   - **Sunny Weather Id**: 晴天天气配置的ID（默认0）
   - **Rainy Weather Id**: 雨天天气配置的ID（默认1）
   - **Transition Curve Id**: 过渡曲线的ID（默认0）
   - **Transition Duration**: 过渡持续时间（默认10秒）

### 2. 配置天气资源
确保在 `WeatherGroup` 中已经配置了对应的天气配置：
- ID为0的晴天配置
- ID为1的雨天配置
- 至少一个过渡曲线配置

### 3. 运行时控制
运行游戏后，你可以通过以下方式控制天气过渡：

#### GUI控制面板
- **开始过渡到雨天**: 启动从当前晴天状态到雨天的过渡
- **重置为晴天**: 立即回到晴天状态
- **直接切换到雨天**: 立即切换到雨天状态
- **停止过渡**: 停止当前正在进行的过渡

#### 代码控制
```csharp
// 获取控制器引用
WeatherTransitionController controller = GetComponent<WeatherTransitionController>();

// 开始过渡
controller.StartTransition();

// 停止过渡
controller.StopTransition();

// 重置为晴天
controller.ResetToSunny();

// 直接设置为雨天
controller.SetToRainy();

// 设置过渡时间
controller.SetTransitionDuration(15f); // 15秒过渡

// 获取过渡进度
float progress = controller.GetTransitionProgress(); // 返回0-1的值

// 检查是否在过渡中
bool isTransitioning = controller.IsTransitioning();
```

## 自动启动功能
如果需要在场景开始时自动启动过渡：
1. 勾选 `Auto Start Transition`
2. 设置 `Auto Start Delay` 为希望的延迟时间

## 配置要求

### TimeOfDay组件要求
- 必须在同一个GameObject上有 `TimeOfDay` 组件
- `TimeOfDay` 必须配置了 `WeatherGroup`
- `WeatherGroup` 必须初始化了天气字典

### WeatherGroup配置要求
- 至少包含两个天气配置（晴天和雨天）
- 至少包含一个过渡曲线配置
- 天气配置ID必须与控制器中设置的ID匹配

## 故障排除

### 常见错误
1. **"未找到TimeOfDay组件"**: 确保在同一个GameObject上添加了TimeOfDay组件
2. **"TimeOfDay组件未设置WeatherGroup"**: 在TimeOfDay的Inspector中设置WeatherGroup
3. **"晴天配置ID不存在"**: 检查WeatherGroup中是否有对应ID的天气配置
4. **"过渡更新失败"**: 检查天气ID和曲线ID是否都存在于WeatherGroup中

### 调试信息
控制器会在Console中输出详细的调试信息，包括：
- 初始化状态
- 过渡开始/结束信息
- 错误信息和原因

## 扩展功能
如果需要添加更多功能，可以考虑：
- 添加更多天气状态的过渡
- 支持循环过渡
- 添加音效控制
- 集成UI系统进行更美观的控制

## 性能注意事项
- 过渡期间会每帧更新天气属性，对性能有一定影响
- 建议在移动设备上适当调整过渡时间和复杂度
- 可以通过TimeOfDay的fixedUpdate选项来优化性能
