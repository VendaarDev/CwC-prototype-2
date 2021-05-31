using Impostors.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Impostors.Jobs
{
    /// <summary>
    /// 
    /// </summary>
    [BurstCompile]
    public struct IsNeedUpdateImpostorsJob : IJobParallelFor
    {
        public NativeArray<ImpostorableObject> impostors;
        public float3 cameraPosition;
        public float3 lightDirection;
        public float multiplier;
        public int screenHeight;
        public float gameTime;

        public void Execute(int index)
        {
//            if (impostors[index].isVisible == 0)
//                return;
            ImpostorableObject curImpostorableObject = impostors[index];
            float3 imposterPosition = curImpostorableObject.data.position;
            float3 fromCamToBill = cameraPosition - imposterPosition;
            float nowDistance = math.length(fromCamToBill);
            float screenSize = curImpostorableObject.data.height / (nowDistance * multiplier);
            var lightDirectionCopy = lightDirection;
            var time = gameTime;
            var screenHeight = this.screenHeight;

            screenSize = math.clamp(screenSize, 0, 1);

            curImpostorableObject.nowScreenSize = screenSize;
            curImpostorableObject.nowDirection = fromCamToBill;
            curImpostorableObject.nowDistance = nowDistance;
            curImpostorableObject.requiredAction = ImpostorableObject.RequiredAction.NotSet;

            if (curImpostorableObject.HasImpostor == false)
            {
                if (curImpostorableObject.nowScreenSize <
                    curImpostorableObject.settings.screenRelativeTransitionHeight &&
                    curImpostorableObject.nowScreenSize >
                    curImpostorableObject.settings.screenRelativeTransitionHeightCull)
                    curImpostorableObject.requiredAction = ImpostorableObject.RequiredAction.GoToImpostorMode;
                goto ExitLabel;
            }

            if (screenSize > curImpostorableObject.settings.screenRelativeTransitionHeight)
            {
                curImpostorableObject.requiredAction = ImpostorableObject.RequiredAction.GoToNormalMode;
                goto ExitLabel;
            }

            if (screenSize < curImpostorableObject.settings.screenRelativeTransitionHeightCull)
            {
                curImpostorableObject.requiredAction = ImpostorableObject.RequiredAction.Cull;
                goto ExitLabel;
            }

            bool needUpdate = IsNeedUpdate();

            bool IsNeedUpdate()
            {
                // check if angle from last update bigger then maxAngleTreshold
                float angle = math.degrees(AngleInRad(curImpostorableObject.lastUpdate.cameraDirection, fromCamToBill));

                float AngleInRad(float3 vec1, float3 vec2)
                {
                    float some = (vec1.x * vec2.x + vec1.y * vec2.y + vec1.z * vec2.z) /
                                 math.sqrt(math.lengthsq(vec1) * math.lengthsq(vec2));
                    if (1 - some < 0.0001f)
                        return 0.0f;
                    some = math.acos(some);
                    some = math.abs(some);
                    return some;
                }

                // if (!_curImposter.isStatic)
                //     _angle += AngleInDeg(_curImposter.objectForwardDirection, _transform.forward);
                if (angle > curImpostorableObject.settings.deltaCameraAngle)
                    return true;


                if (curImpostorableObject.settings.useUpdateByTime == 1 &&
                    (time - curImpostorableObject.lastUpdate.time) > curImpostorableObject.settings.timeInterval)
                    return true;

                // If light angle has changed
                if (curImpostorableObject.settings.useDeltaLightAngle == 1)
                {
                    angle = AngleInRad(curImpostorableObject.lastUpdate.lightDirection, lightDirectionCopy) *
                            Mathf.Rad2Deg;
                    if (angle > curImpostorableObject.settings.deltaLightAngle)
                        return true;
                }


                // if need to change resolution of imposter texture
                int maxTexRes = curImpostorableObject.settings.maxTextureResolution;

                int resolution = (int) (screenHeight * screenSize);
                if (resolution >= maxTexRes)
                    resolution = maxTexRes;
                else if (resolution <= curImpostorableObject.settings.minTextureResolution)
                    resolution = curImpostorableObject.settings.minTextureResolution;
                else
                    resolution = math.ceilpow2(resolution);

                if (resolution > curImpostorableObject.lastUpdate.textureResolution)
                    return true;


                // if size on screen changed 
                float distance = math.abs(nowDistance - curImpostorableObject.lastUpdate.distance) /
                                 curImpostorableObject.lastUpdate.distance;
                if (distance > curImpostorableObject.settings.deltaDistance)
                    return true;

                return false;
            }

            curImpostorableObject.requiredAction = needUpdate
                ? ImpostorableObject.RequiredAction.UpdateImpostorTexture
                : ImpostorableObject.RequiredAction.None;

            ExitLabel:
            impostors[index] = curImpostorableObject;
        }
    }
}