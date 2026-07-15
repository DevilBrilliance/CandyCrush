$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$jsonPath = 'c:\Users\22734\Desktop\unity\CandyCrush\Docs\_delivery_content.json'
$data = Get-Content -LiteralPath $jsonPath -Encoding UTF8 -Raw | ConvertFrom-Json

function Esc([string]$s) {
  if ($null -eq $s) { return '' }
  return ($s -replace '&','&amp;' -replace '<','&lt;' -replace '>','&gt;' -replace '"','&quot;')
}
function PXml([string]$text, [bool]$bold=$false, [string]$size='21') {
  $b = if ($bold) { '<w:b/>' } else { '' }
  return @"
<w:p><w:pPr><w:spacing w:after="120"/></w:pPr><w:r><w:rPr>$b<w:sz w:val="$size"/><w:szCs w:val="$size"/></w:rPr><w:t xml:space="preserve">$(Esc $text)</w:t></w:r></w:p>
"@
}
function TableXml($t) {
  if ($null -eq $t) { return '' }
  $rows = [int]$t.rows; $cols = [int]$t.cols
  $cells = @($t.cells)
  $sb = New-Object System.Text.StringBuilder
  [void]$sb.Append('<w:tbl><w:tblPr><w:tblW w:w="5000" w:type="pct"/><w:tblBorders>')
  foreach ($e in @('top','left','bottom','right','insideH','insideV')) {
    [void]$sb.Append("<w:$e w:val=`"single`" w:sz=`"4`" w:space=`"0`" w:color=`"666666`"/>")
  }
  [void]$sb.Append('</w:tblBorders></w:tblPr>')
  $i = 0
  for ($r=0; $r -lt $rows; $r++) {
    [void]$sb.Append('<w:tr>')
    for ($c=0; $c -lt $cols; $c++) {
      $isHeader = ($r -eq 0)
      $bold = if ($isHeader) { '<w:b/>' } else { '' }
      $shade = if ($isHeader) { '<w:shd w:val="clear" w:fill="E8EEF7"/>' } else { '' }
      $txt = Esc ([string]$cells[$i]); $i++
      [void]$sb.Append("<w:tc><w:tcPr>$shade<w:tcW w:w=`"1`" w:type=`"auto`"/></w:tcPr><w:p><w:r><w:rPr>$bold<w:sz w:val=`"18`"/></w:rPr><w:t xml:space=`"preserve`">$txt</w:t></w:r></w:p></w:tc>")
    }
    [void]$sb.Append('</w:tr>')
  }
  [void]$sb.Append('</w:tbl>')
  [void]$sb.Append((PXml '' $false '18'))
  return $sb.ToString()
}
function Emit-Block($block, [int]$level, [System.Text.StringBuilder]$body) {
  if ($null -eq $block) { return }
  if ($block.h) {
    $sz = switch ($level) { 1 { '28' } 2 { '24' } default { '22' } }
    [void]$body.Append((PXml ([string]$block.h) $true $sz))
  }
  foreach ($x in @($block.p)) { if ($x) { [void]$body.Append((PXml ([string]$x))) } }
  foreach ($t in @($block.tables)) { [void]$body.Append((TableXml $t)) }
  foreach ($x in @($block.p2)) { if ($x) { [void]$body.Append((PXml ([string]$x))) } }
  foreach ($t in @($block.tables2)) { [void]$body.Append((TableXml $t)) }
  foreach ($x in @($block.p3)) { if ($x) { [void]$body.Append((PXml ([string]$x))) } }
  foreach ($t in @($block.tables3)) { [void]$body.Append((TableXml $t)) }
  foreach ($h2 in @($block.h2)) { Emit-Block $h2 ($level+1) $body }
  foreach ($h3 in @($block.h3)) { Emit-Block $h3 ($level+1) $body }
}

$body = New-Object System.Text.StringBuilder
[void]$body.Append((PXml ([string]$data.title) $true '32'))
foreach ($x in @($data.intro)) { [void]$body.Append((PXml ([string]$x))) }
foreach ($sec in @($data.sections)) { Emit-Block $sec 1 $body }

$documentXml = @"
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    $($body.ToString())
    <w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1134" w:right="1134" w:bottom="1134" w:left="1134"/></w:sectPr>
  </w:body>
</w:document>
"@

$contentTypes = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>
'@
$rels = @'
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
'@

$outs = @(
  (Join-Path $env:USERPROFILE 'Desktop\程序开发二测_架构设计线_交付说明.docx'),
  'c:\Users\22734\Desktop\unity\CandyCrush\Docs\程序开发二测_架构设计线_交付说明.docx',
  'c:\Users\22734\Desktop\unity\CandyCrush\Docs\Exam2_Architecture_Delivery.docx'
)
$tmpDir = Join-Path $env:TEMP ('docxbuild_' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $tmpDir 'word') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $tmpDir '_rels') -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $tmpDir '[Content_Types].xml'), $contentTypes, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $tmpDir '_rels\.rels'), $rels, [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $tmpDir 'word\document.xml'), $documentXml, [System.Text.UTF8Encoding]::new($false))

$zipPath = Join-Path $env:TEMP 'CandyCrush_Exam2_Delivery.docx'
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
$fs = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::Create)
$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)
function Add-Entry([string]$name, [string]$content) {
  $e = $zip.CreateEntry($name, [System.IO.Compression.CompressionLevel]::Optimal)
  $w = New-Object System.IO.StreamWriter($e.Open(), [System.Text.UTF8Encoding]::new($false))
  $w.Write($content)
  $w.Dispose()
}
Add-Entry '[Content_Types].xml' $contentTypes
Add-Entry '_rels/.rels' $rels
Add-Entry 'word/document.xml' $documentXml
$zip.Dispose()
$fs.Dispose()
Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue

foreach ($o in $outs) {
  $dir = Split-Path $o -Parent
  if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
  Copy-Item $zipPath $o -Force
}
Get-Item $outs | Format-Table FullName, Length, LastWriteTime

$z = [System.IO.Compression.ZipFile]::OpenRead($outs[1])
$z.Entries | ForEach-Object { $_.FullName }
$e = $z.GetEntry('word/document.xml')
$sr = New-Object System.IO.StreamReader($e.Open())
$xml = $sr.ReadToEnd(); $sr.Close(); $z.Dispose()
Write-Host 'doc xml chars' $xml.Length
Write-Host 'AudioController' ($xml.Contains('AudioController'))
Write-Host 'SpriteAdditive' ($xml.Contains('SpriteAdditive'))
Write-Host 'Tween' ($xml.Contains('Tween'))
Write-Host '第五部分' ($xml.Contains('第五部分'))
