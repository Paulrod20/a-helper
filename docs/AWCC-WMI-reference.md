# AWCCWmiMethodFunction — Reverse-Engineering Reference

Research date: 2026-07-11. Target: Alienware x16 R1, Windows 11, WinForms/.NET 10.
Method: 5-angle web research, 20 source-claim extractions, 75 adversarial verification votes
(58 upheld / 17 refuted).

---

## 0. The single best source

**Official Linux kernel documentation for the `alienware-wmi` driver.**

- Docs: https://docs.kernel.org/wmi/devices/alienware-wmi.html
- Raw:  https://www.kernel.org/doc/Documentation/wmi/devices/alienware-wmi.rst
- Source: `drivers/platform/x86/dell/alienware-wmi-wmax.c`

This documents **the exact WMI class we found on the machine** — `AWCCWmiMethodFunction`,
GUID `{A70591CE-A997-11DA-B012-B622A1EF5492}`. It was written by Kurt Borja, reverse-engineered
from AWCC, reviewed by kernel maintainers, and **merged into mainline Linux**. That review-and-merge
process is why it beats every forum post and community repo: it's the only source that survived
adversarial scrutiny on essentially every claim.

**Rule of thumb: when any other source disagrees with the kernel docs, the kernel docs win.**
(Verified repeatedly below — several popular community sources are wrong.)

---

## 1. The argument encoding (CONFIRMED, high confidence)

Every method takes `[in] uint32 arg2` and returns `[out] uint32 argr`.

`arg2` is **plain byte-packing** — not a bitfield abstraction:

```
Byte 0 (bits  0-7 ) = operation code
Byte 1 (bits  8-15) = first argument  (profile ID, fan ID, sensor ID)
Byte 2 (bits 16-23) = second argument (boost value)
Byte 3 (bits 24-31) = third argument  (rarely used)
```

Canonical example from the kernel docs: operation `0x01` with ID `0xA0` → `arg2 = 0xA001`.

In C#:
```csharp
uint arg2 = (uint)(operation | (arg1 << 8) | (arg2b << 16));
```

**Return value `0xFFFFFFFF` means failure / unsupported operation.** Check for this on every call.

---

## 2. Method map (CONFIRMED)

| Method | WmiMethodId | Purpose |
|---|---|---|
| `GetFanSensors` | 19 | Fan→sensor associations |
| `Thermal_Information` | 20 (0x14) | All reads: temps, RPM, current profile |
| `Thermal_Control` | 21 (0x15) | All writes: set profile, set fan boost |
| `GameShiftStatus` | 37 | Toggle/query Game Shift (G-Series) |

### `Thermal_Information` (reads) — operations

| Op (Byte 0) | Description | Args |
|---|---|---|
| `0x02` | Get system description | — returns Byte0=#fans, Byte1=#sensors, Byte2=unknown, Byte3=#profiles |
| `0x03` | Get resource ID (enumerate fans/sensors/profiles) | Byte 1: index |
| `0x04` | Get current temperature | Byte 1: Sensor ID |
| `0x05` | Get current RPM | Byte 1: Fan ID |
| `0x06` | Get fan speed **percentage** | Byte 1: Fan ID — *not implemented on every model* |
| `0x0B` | Get current thermal profile ID | — |

### `Thermal_Control` (writes) — operations

| Op (Byte 0) | Description | Args |
|---|---|---|
| `0x01` | **Activate a thermal profile** | Byte 1: Profile ID |
| `0x02` | **Set fan boost** | Byte 1: Fan ID, Byte 2: Boost value |

This is the mode-vs-fan distinction you asked about: **op 0x01 = performance mode,
op 0x02 = fan speed.** Same method, different low byte.

### `GetFanSensors` — operations

| Op | Description | Args |
|---|---|---|
| `0x01` | Sensor count for a fan | Byte 1: Fan ID |
| `0x02` | List sensor IDs for a fan | Byte 1: Fan ID, Byte 2: index |

---

## 3. Thermal profile IDs (CONFIRMED — but see the trap)

There are **two mutually exclusive sets**. A given machine supports Legacy *or* USTT
(User Selectable Thermal Tables) — **never both.**

**Legacy profiles:**
| Mode | Hex | Dec |
|---|---|---|
| Quiet | `0x96` | 150 |
| Balanced | `0x97` | 151 |
| Balanced Performance | `0x98` | 152 |
| Performance | `0x99` | 153 |

**USTT profiles:**
| Mode | Hex | Dec |
|---|---|---|
| Balanced | `0xA0` | 160 |
| Balanced Performance | `0xA1` | 161 |
| Cool | `0xA2` | 162 |
| Quiet | `0xA3` | 163 |
| Performance | `0xA4` | 164 |
| Low Power | `0xA5` | 165 |

**Special (both sets):**
| Mode | Hex | Dec |
|---|---|---|
| Custom | `0x00` | 0 |
| G-Mode | `0xAB` | 171 |

Notes:
- **Custom (0x00) is supported on every model.**
- **G-Mode (0xAB) replaces Performance on G-Series/G-Mode-capable machines** — it is not
  simply a 12th independent mode.

### ⚠️ TRAP: the popular community enum has WRONG NAMES

`AlexIII/tcc-g15`'s `WMI-AWCC-doc.md` is the most-cited community source (Lumine and others are
built on it). Its **numbers are right but two names are wrong**:

| Value | tcc-g15 says | Kernel docs say (CORRECT) |
|---|---|---|
| 152 / `0x98` | ~~Performance~~ | **Balanced Performance** |
| 153 / `0x99` | ~~FullSpeed~~ | **Performance** |

**There is no "FullSpeed" mode.** The USTT names diverge similarly (tcc-g15 calls 164
"FullSpeed" and 165 "BatterySaver"; kernel says "Performance" and "Low Power").

If you copy tcc-g15's enum, your "Performance" button will silently select **Balanced
Performance** instead. Use the kernel naming.

Note tcc-g15 only marks `Custom (0)`, `Balanced (151)`, `G_Mode (171)` as personally
confirmed-working — the rest are untested even in that source.

---

## 4. Fan control — important semantic caveat (CONFIRMED)

**You cannot set an absolute fan speed on modern Alienware hardware.** `Thermal_Control` op `0x02`
sets a **boost above the BIOS/EC baseline**, not a duty cycle.

The Linux driver models it as:
```
pwm = pwm_base + (pwm_boost / 255) * (pwm_max - pwm_base)
```
(alienware-wmi-wmax driver; boost is 0–255 in the kernel hwmon abstraction. Community Windows
tools like alienfx-tools expose boost as 0–100 percent. **Verify the accepted range empirically
on the x16 R1** — sources disagree on 0-100 vs 0-255, and this was NOT independently corroborated.)

From `alienfx-tools` docs, verbatim: *"ACPI calls can't control fans directly for modern gear
(but can set direct fan percent at older one), so all you can do is set fan boost (Increase RPM
above BIOS level)."*

**The BIOS/EC will override your setting if temperatures become unsafe.** This is a feature, not
a bug — it means you cannot cook the machine by setting fans to 0. Lumine describes its own fan
control as only "semi-manual" for this reason.

### Fan / sensor ID ranges (from tcc-g15 — single-sourced, verify at runtime)
- Sensor IDs: 1–48
- Fan IDs: 49–99
- Observed fan ordering: `{50, 59, 51, 60}`

Don't hardcode these. Enumerate them (see §6).

---

## 5. GPU MUX switching — ⚠️ UNDOCUMENTED, DEAD END

**No source found documents `MUXSwitch` arg2 values. Not one.**

- Kernel docs: `MUXSwitch` is **not covered at all**.
- tcc-g15's doc: **no MUXSwitch entry whatsoever** (verified by direct file inspection).
- No community project, forum post, or gist surfaced any values.

What *is* known: the x16 R1 BIOS has a **"Hybrid Graphics / Advanced Optimus"** toggle
(default: On). When off, the dGPU drives all displays. Whether the runtime `MUXSwitch` WMI method
is the same mechanism as this BIOS setting is **unverified**, as is whether a reboot is required.

**Recommendation: treat MUX switching as a research project of its own, not a v1 feature.**
Ship performance modes + fan control first. If you pursue MUX, you'll likely need to trace what
AWCC itself sends (e.g. WMI tracing / ETW, or decompiling AWCC's .NET assemblies) rather than
find it documented.

---

## 6. THE KEY DESIGN INSIGHT: enumerate, don't hardcode

Because Legacy-vs-USTT is model-dependent and **we have not confirmed which set the x16 R1 uses**,
do not ship a hardcoded profile list. The interface is self-describing — use it:

1. `Thermal_Information(0x02)` → system description. Byte 3 = **number of thermal profiles**,
   Byte 0 = number of fans, Byte 1 = number of sensors.
2. `Thermal_Information(0x03 | index<<8)` → enumerate **resource IDs** (fans, sensors, profiles).
3. Match returned profile IDs against the known tables in §3 to discover whether this machine is
   Legacy or USTT, and which modes it actually supports.
4. `Thermal_Information(0x0B)` → read back the currently-active profile (use this to confirm a
   `Thermal_Control` write actually took effect — don't trust the return value alone).

This gives you a `PowerModeService` that adapts to the hardware instead of guessing, and it
doubles as your discovery tool: **run the enumeration first, and it will tell you empirically
whether the x16 R1 is Legacy or USTT.** That's the single highest-value first thing to build.

---

## 7. Safety caveats (from community sources)

1. **`Power control` set to non-zero can LOCK OUT fan control** (`alienfan-tools`). If fan control
   mysteriously stops responding, this is a prime suspect. Be cautious with `PowerInformation`.
2. **BIOS/EC overrides unsafe fan settings** — you cannot force fans low enough to damage the machine.
3. **Model gating is real.** The kernel driver gates thermal support behind a per-model quirk table;
   there's an explicit entry for **"Alienware x15 R1"**. The **x16 R1 is not confirmed present
   upstream** — meaning our machine's exact behavior isn't guaranteed to match, and is another
   reason to enumerate rather than assume.
4. **Always check for `0xFFFFFFFF`** (failure sentinel) on every call.
5. Verify writes by reading back (`0x0B`), not by trusting the return code.

---

## 8. Sources that MISLED (do not trust these)

| Source | Problem |
|---|---|
| `AlexIII/tcc-g15` `WMI-AWCC-doc.md` | Numbers right, **two mode names wrong** (see §3). Targets Dell G15, not x16 R1. No MUXSwitch coverage. |
| `Hugo2049/alienware-16x-fan-control` | Codes (0xA0=Balanced, 0xA1=**Performance**, 0xA3=Quiet) **conflict with kernel** (0xA1 = Balanced *Performance*; Performance = 0xA4). Tested on **Alienware 16X Aurora AC16251** — a different, newer machine than the x16 R1. |
| LKML **v3 draft** patches (Oct 2024) | Symbol names `WMAX_METHOD_THERMAL_*`, `WMAX_ARG_GET_CURRENT_PROF` and the `FIELD_PREP(PROFILE_MASK)\|PROFILE_ACTIVATE` / `GENMASK(15,8)\|BIT(0)` packing scheme **did not survive review**. Merged kernel uses `AWCC_`-prefixed names and plain byte-packing. The numeric values (0x14/0x15/0x0B) are still correct. |
| `alienfan-tools` README claim that it "doesn't use WMI" | Misleading dichotomy — `AWCCWmiMethodFunction` is itself an ACPI-backed WMI wrapper (`_SB.AMWW.WMAX` / `AWCCTABL` SSDT). WMI and "raw ACPI" reach the **same** firmware code. Good news: it means community ACPI findings are *transferable* to the WMI path. |

---

## 9. Suggested build order

1. **`ThermalQueryService`** (read-only, zero risk): enumerate system description + resource IDs +
   current profile + fan RPM. **This answers Legacy-vs-USTT empirically.** Build this first.
2. **`PowerModeService`**: `Thermal_Control(0x01 | profileId<<8)`, verified by reading back `0x0B`.
3. **`FanService`**: `Thermal_Control(0x02 | fanId<<8 | boost<<16)`. Determine boost range empirically.
4. **MUX switching**: defer. Undocumented; needs original reverse-engineering.

WMI access from C#: `System.Management` (`ManagementObject.InvokeMethod`) against
`root\wmi` → `AWCCWmiMethodFunction`. **Requires elevation (admin).**
