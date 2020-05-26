# Smooth Step Tween

Smooth step interpolation between 2 values within a time interval.

## Ports

### Inputs

Port Name|Type|Default Value|Description
---|---|---|---
**Start**|_Trigger_||Trigger to start the interpolation.
**Stop**|_Trigger_||Trigger to stop and reset the interpolation.
**Pause**|_Trigger_||Trigger to pause the interpolation.
**Reset**|_Trigger_||Resets the internal timer.
**From**|_[Float]_||The value to interpolate from.
**To**|_[Float]_||The value to interpolate to.
**Duration**|_[Float]_||The duration of the interpolation (in seconds).
### Outputs

Port Name|Type|Default Value|Description
---|---|---|---
**OnDone**|_Trigger_||Fires when the interpolation runs to completion (i.e. Stop is not triggered).
**OnFrame**|_Trigger_||Fires every frame the interpolation runs (i.e. not while paused).
**Result**|_[Float]_|0|The interpolated value between To and From at the current time.
