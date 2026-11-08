param(
    [string]$Dir = 'D:\Unity\WECS-BATTLE\Client\Assets\AssetRaws\AnimationClips\Ya',
    [string[]]$Names = @('Ani_SwitchIn_Attack', 'Ani_SwitchIn_Attack_Ex'),
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$inv = [System.Globalization.CultureInfo]::InvariantCulture
$enc = New-Object System.Text.UTF8Encoding($false)

function Fmt([double]$v) {
    $s = $v.ToString('0.##########', $inv)
    if ($s -eq '-0') { $s = '0' }
    return $s
}

function Get-Axis($k, [int]$a) {
    if ($a -eq 0) { return [double]$k.X }
    if ($a -eq 1) { return [double]$k.Y }
    return [double]$k.Z
}

function SampleAxis($keys, [int]$a, [double]$time) {
    $n = $keys.Count
    if ($n -eq 0) { return 0.0 }
    if ($time -le $keys[0].T) { return (Get-Axis $keys[0] $a) }
    if ($time -ge $keys[$n - 1].T) { return (Get-Axis $keys[$n - 1] $a) }
    for ($i = 0; $i -lt $n - 1; $i++) {
        if ($time -ge $keys[$i].T -and $time -le $keys[$i + 1].T) {
            $dt = [double]$keys[$i + 1].T - [double]$keys[$i].T
            if ($dt -le 0) { return (Get-Axis $keys[$i + 1] $a) }
            $r = ($time - [double]$keys[$i].T) / $dt
            return ((Get-Axis $keys[$i] $a) + ((Get-Axis $keys[$i + 1] $a) - (Get-Axis $keys[$i] $a)) * $r)
        }
    }
    return (Get-Axis $keys[$n - 1] $a)
}

function Get-PositionBlocks($lines, [int]$start, [int]$end) {
    $blocks = @()
    $curStart = -1
    $times = @()
    $xs = @(); $ys = @(); $zs = @()
    $path = ''

    $flush = {
        if ($script:curStart -ge 0 -and $script:path -ne '' -and $script:times.Count -gt 0) {
            $keys = @()
            for ($k = 0; $k -lt $script:times.Count; $k++) {
                $keys += [pscustomobject]@{ T = $script:times[$k]; X = $script:xs[$k]; Y = $script:ys[$k]; Z = $script:zs[$k] }
            }
            $script:blocks += [pscustomobject]@{ Path = $script:path; Start = $script:curStart; End = $script:blockEnd; Keys = $keys }
        }
    }

    for ($i = $start; $i -lt $end; $i++) {
        $l = $lines[$i]
        if ($l -eq '  - curve:') {
            if ($curStart -ge 0 -and $path -ne '' -and $times.Count -gt 0) {
                $keys = @()
                for ($k = 0; $k -lt $times.Count; $k++) { $keys += [pscustomobject]@{ T = $times[$k]; X = $xs[$k]; Y = $ys[$k]; Z = $zs[$k] } }
                $blocks += [pscustomobject]@{ Path = $path; Start = $curStart; End = $i - 1; Keys = $keys }
            }
            $curStart = $i; $times = @(); $xs = @(); $ys = @(); $zs = @(); $path = ''
        }
        elseif ($l -match 'time: (\S+)$') { $times += [double]::Parse($Matches[1], $inv) }
        elseif ($l -match 'value: \{x: (\S+), y: (\S+), z: (\S+)\}') {
            $xs += [double]::Parse($Matches[1], $inv)
            $ys += [double]::Parse($Matches[2], $inv)
            $zs += [double]::Parse($Matches[3], $inv)
        }
        elseif ($l -match '^    path: (.*)$') { $path = $Matches[1] }
    }
    if ($curStart -ge 0 -and $path -ne '' -and $times.Count -gt 0) {
        $keys = @()
        for ($k = 0; $k -lt $times.Count; $k++) { $keys += [pscustomobject]@{ T = $times[$k]; X = $xs[$k]; Y = $ys[$k]; Z = $zs[$k] } }
        $blocks += [pscustomobject]@{ Path = $path; Start = $curStart; End = $end - 1; Keys = $keys }
    }
    return $blocks
}

function Get-Slopes($keys, [int]$a) {
    $n = $keys.Count
    $s = @()
    for ($i = 0; $i -lt $n; $i++) { $s += 0.0 }
    if ($n -lt 2) { return $s }
    for ($i = 0; $i -lt $n; $i++) {
        if ($i -eq 0) { $dt = [double]$keys[1].T - [double]$keys[0].T; $s[$i] = if ($dt -le 0) { 0.0 } else { ((Get-Axis $keys[1] $a) - (Get-Axis $keys[0] $a)) / $dt } }
        elseif ($i -eq $n - 1) { $dt = [double]$keys[$n - 1].T - [double]$keys[$n - 2].T; $s[$i] = if ($dt -le 0) { 0.0 } else { ((Get-Axis $keys[$n - 1] $a) - (Get-Axis $keys[$n - 2] $a)) / $dt } }
        else { $dt = [double]$keys[$i + 1].T - [double]$keys[$i - 1].T; $s[$i] = if ($dt -le 0) { 0.0 } else { ((Get-Axis $keys[$i + 1] $a) - (Get-Axis $keys[$i - 1] $a)) / $dt } }
    }
    return $s
}

function New-PositionBlock($keys, [string]$path) {
    $sx = Get-Slopes $keys 0
    $sy = Get-Slopes $keys 1
    $sz = Get-Slopes $keys 2
    $out = @('  - curve:', '      serializedVersion: 2', '      m_Curve:')
    for ($i = 0; $i -lt $keys.Count; $i++) {
        $out += '      - serializedVersion: 3'
        $out += "        time: $(Fmt $keys[$i].T)"
        $out += "        value: {x: $(Fmt $keys[$i].X), y: $(Fmt $keys[$i].Y), z: $(Fmt $keys[$i].Z)}"
        $out += "        inSlope: {x: $(Fmt $sx[$i]), y: $(Fmt $sy[$i]), z: $(Fmt $sz[$i])}"
        $out += "        outSlope: {x: $(Fmt $sx[$i]), y: $(Fmt $sy[$i]), z: $(Fmt $sz[$i])}"
        $out += '        tangentMode: 0'
        $out += '        weightedMode: 0'
        $out += '        inWeight: {x: 0.33333334, y: 0.33333334, z: 0.33333334}'
        $out += '        outWeight: {x: 0.33333334, y: 0.33333334, z: 0.33333334}'
    }
    $out += '      m_PreInfinity: 2'
    $out += '      m_PostInfinity: 2'
    $out += '      m_RotationOrder: 4'
    $out += "    path: $path"
    return $out
}

function New-EditorEntry($keys, [int]$axis, [string]$attr, [string]$path) {
    $s = Get-Slopes $keys $axis
    $out = @('  - serializedVersion: 2', '    curve:', '      serializedVersion: 2', '      m_Curve:')
    for ($i = 0; $i -lt $keys.Count; $i++) {
        $out += '      - serializedVersion: 3'
        $out += "        time: $(Fmt $keys[$i].T)"
        $out += "        value: $(Fmt (Get-Axis $keys[$i] $axis))"
        $out += "        inSlope: $(Fmt $s[$i])"
        $out += "        outSlope: $(Fmt $s[$i])"
        $out += '        tangentMode: 0'
        $out += '        weightedMode: 0'
        $out += '        inWeight: 0.33333334'
        $out += '        outWeight: 0.33333334'
    }
    $out += '      m_PreInfinity: 2'
    $out += '      m_PostInfinity: 2'
    $out += '      m_RotationOrder: 4'
    $out += "    attribute: $attr"
    $out += "    path: $path"
    $out += '    classID: 4'
    $out += '    script: {fileID: 0}'
    $out += '    flags: 0'
    return $out
}

foreach ($name in $Names) {
    $f = Join-Path $Dir "$name.anim"
    if (-not (Test-Path $f)) { "MISSING $name"; continue }

    $t = [IO.File]::ReadAllText($f)
    $nl = if ($t.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($l in ($t -split "\r?\n")) { $lines.Add($l) }

    $ps = $lines.IndexOf('  m_PositionCurves:')
    $ss = $lines.IndexOf('  m_ScaleCurves:')
    $blocks = Get-PositionBlocks $lines $ps $ss
    $rootB = $blocks | Where-Object { $_.Path -eq 'Armature/Root' } | Select-Object -First 1
    $bipB = $blocks | Where-Object { $_.Path -eq 'Armature/Root/Bip001' } | Select-Object -First 1
    if ($null -eq $rootB -or $null -eq $bipB) { "SKIP(no block) $name"; continue }

    $bip0x = [double]$bipB.Keys[0].X
    $bip0y = [double]$bipB.Keys[0].Y
    $bip0z = [double]$bipB.Keys[0].Z

    $times = New-Object System.Collections.Generic.List[double]
    foreach ($k in $rootB.Keys) {
        $dup = $false
        foreach ($x in $times) { if ([Math]::Abs($x - [double]$k.T) -lt 1e-6) { $dup = $true; break } }
        if (-not $dup) { $times.Add([double]$k.T) }
    }
    foreach ($k in $bipB.Keys) {
        $dup = $false
        foreach ($x in $times) { if ([Math]::Abs($x - [double]$k.T) -lt 1e-6) { $dup = $true; break } }
        if (-not $dup) { $times.Add([double]$k.T) }
    }
    $times.Sort()

    $newKeys = @()
    foreach ($tt in $times) {
        $rx = SampleAxis $rootB.Keys 0 $tt
        $ry = SampleAxis $rootB.Keys 1 $tt
        $rz = SampleAxis $rootB.Keys 2 $tt
        $bx = SampleAxis $bipB.Keys 0 $tt
        $by = SampleAxis $bipB.Keys 1 $tt
        $bz = SampleAxis $bipB.Keys 2 $tt
        $newKeys += [pscustomobject]@{ T = $tt; X = ($rx + $bx - $bip0x); Y = ($ry + $by - $bip0y); Z = ($rz + $bz - $bip0z) }
    }

    $constKeys = @()
    foreach ($k in $bipB.Keys) { $constKeys += [pscustomobject]@{ T = [double]$k.T; X = $bip0x; Y = $bip0y; Z = $bip0z } }

    $repl = @()
    $repl += [pscustomobject]@{ Start = $rootB.Start; End = $rootB.End; Lines = (New-PositionBlock $newKeys 'Armature/Root') }
    $repl += [pscustomobject]@{ Start = $bipB.Start; End = $bipB.End; Lines = (New-PositionBlock $constKeys 'Armature/Root/Bip001') }

    $ec = $lines.IndexOf('  m_EditorCurves:')
    $eu = $lines.IndexOf('  m_EulerEditorCurves:')
    $starts = @()
    for ($i = $ec + 1; $i -lt $eu; $i++) { if ($lines[$i] -eq '  - serializedVersion: 2') { $starts += $i } }
    $entryCount = 0
    for ($s = 0; $s -lt $starts.Count; $s++) {
        $b = $starts[$s]
        $e = if ($s + 1 -lt $starts.Count) { $starts[$s + 1] - 1 } else { $eu - 1 }
        $attr = ''; $p = ''
        for ($i = $b; $i -le $e; $i++) {
            if ($lines[$i] -match '^    attribute: (.*)$') { $attr = $Matches[1] }
            if ($lines[$i] -match '^    path: (.*)$') { $p = $Matches[1] }
        }
        if ($attr -notmatch '^m_LocalPosition\.[xyz]$') { continue }
        if ($p -ne 'Armature/Root' -and $p -ne 'Armature/Root/Bip001') { continue }
        $axis = switch ($attr) { 'm_LocalPosition.x' { 0 } 'm_LocalPosition.y' { 1 } default { 2 } }
        $keys = if ($p -eq 'Armature/Root') { $newKeys } else { $constKeys }
        $repl += [pscustomobject]@{ Start = $b; End = $e; Lines = (New-EditorEntry $keys $axis $attr $p) }
        $entryCount++
    }

    if ($DryRun) {
        "DRYRUN $name unionKeys=$($newKeys.Count) rootKeys=$($rootB.Keys.Count) bipKeys=$($bipB.Keys.Count) editorEntries=$entryCount"
        $rlast = $rootB.Keys[$rootB.Keys.Count - 1]
        $blast = $bipB.Keys[$bipB.Keys.Count - 1]
        "   root: z $($rlast.Z) -> $($newKeys[$newKeys.Count-1].Z) ; bip: z $($blast.Z) -> $bip0z"
        continue
    }

    foreach ($r in ($repl | Sort-Object Start -Descending)) {
        $lines.RemoveRange($r.Start, $r.End - $r.Start + 1)
        $lines.InsertRange($r.Start, [string[]]$r.Lines)
    }

    [IO.File]::WriteAllText($f, [string]::Join($nl, $lines), $enc)
    "MOVED $name unionKeys=$($newKeys.Count) editorEntries=$entryCount"
}
