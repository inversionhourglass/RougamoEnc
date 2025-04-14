# Rougamo EnC

在探索编译时 AOP 支持热重载的路上不断采坑。

## 前世

基于 Rougamo 3.0 版本后使用代理调用的特殊体质，尝试通过 CLR Profiler API 修改方法入口地址使 Rougamo 支持热重载，但最终发现 CLR Profiler 无法与热重载同时使用，前功尽弃！

## 现世之再陷停滞

在了解到 [Harmony](https://github.com/pardeike/Harmony) 这个项目后，再结合 Rougamo 的特殊体质，产生了通过运行时重定向使 Rougamo 支持热重载。

> 当前示例代码使用 **MonoMod** 完成重定向操作， Harmony 底层也是使用 MonoMod 进行实现

### 思路

> 注：设有方法`M`，在应用 Rougamo 后，原方法`M`将被转移到`$Rougamo_M`中，`M`本身将进行 IL 重写注入 AOP 操作，后续步骤中`M`为 AOP 改造后的方法

1. 应用启动初始化时，将`M`拷贝一份存为`$M`。（可优化为编译时拷贝生成）
2. 通过`MetadataUpdateHandler`监听热重载事件。
3. 热重载发生后，`M`的`IL`已发生改变，执行以下一系列操作：
    3.1. 将此时的`M`拷贝一份存为`_M`。
    3.2. 将`M`重定向到`$M`
    3.3. 将`$Rougamo_M`重定向到`_M`

通过上面两次重定向即可在保留 Rougamo 的 AOP 逻辑的同时应用热重载更新的最新代码。

### 如何理解这两次重定向

首先，理解 Rougamo 在编译时的操作。Rougamo 在编译时会将`M`克隆转移到`$Rougamo_M`中，`M`变成执行 AOP 代码然后调用`$Rougamo_M`。
```csharp
// Rougamo 操作前
public void M()
{
    Console.WriteLine("M");
}

// Rougamo 操作后
public void M()
{
    // 伪代码
    var mo = new XxAttribute();
    var context = new MethodContext();
    mo.OnEntry(context);
    try
    {
        $Rougamo_M();
        mo.OnSuccess(context);
    }
    catch (Exception ex)
    {
        mo.OnException(context);
        throw;
    }
    finally
    {
        mo.OnExit(context);
    }
}

private void $Rougamo_M()
{
    Console.WriteLine("M");
}
```
在热重载发生后，`M`将会指向热重载后的新代码，所以上面第一步在应用启动时将`M`拷贝一份存在`$M`是为了将 Rougamo 修改后的`M`备份一份。在热重载发生后，将`M`重定向到`$M`，那么就能让 AOP 代码继续生效。然后`$M`中会调用`$Rougamo_M`，所以只需要再将`$Rougamo_M`重定向到热重载更新后的`M`（也就是步骤 3.1 拷贝的`_M`）就行了。

### 没那么简单

最开始之所以会考虑使用 CLR Profiler API，是因为它提供了更为底层的接口，可操作性更好。如果在托管的应用层面，即使借助 unsafe，能操作的部分还是太少。目前的示例能够一定程度上支持热重载，但是却存在很多问题：
1. 无法调试更新后的代码。热重载发生后会更新调试信息，重定向后`M`的调试信息对应不上，目前不知道托管代码如何能在运行时操作调试信息。
2. 当前调试的方法无法即时生效。如果热重载修改的方法时当前正在调试的方法，那么重定向后是无法让热重载代码即时生效的，需要调整调试指针和当前调用方法堆栈等信息，托管代码如何实现呢？
3. 写一半忘了...好像还有，但是写着写着就忘了，就这样吧，反正就是实现不了。

## 没那么严格

在如何支持热重载这方面，可以尽情的脑洞大开，随便尝试各种不安全的操作，这并不是 Rougamo 不注重安全，而是启用热重载的环境只有开发环境，即使存在一些不安全的操作甚至时一些严重 BUG，也是可以接受的。先落实一种可以实现的思路，这些严重 BUG 都是可以慢慢再去修复的。如果连开发环境出现一些 BUG 都无法接受，那么就不要尝试任何开源项目了。
