using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Motor de Texturizado Procedural PBR para MechaQuack.
/// Genera proceduralmente mapas _BaseMap (albedo), _BumpMap (normales por gradiente Sobel),
/// _MetallicGlossMap (R=Metallic, G=Occlusion, A=Smoothness) y _EmissionMap en shaders URP/Lit.
/// Cumple estrictamente con las reglas Zero External Assets y Cero GC Allocations en juego.
/// </summary>
public static class ProceduralTextureGenerator
{
    private const string TEXTURE_DIR = "Assets/Textures/Generated";
    private const string MATERIAL_DIR = "Assets/Materials/Generated";

    private static readonly Dictionary<string, Material> _materialCache = new Dictionary<string, Material>();
    private static readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

#if UNITY_EDITOR
    [MenuItem("Mecha/Generate Procedural Textures & Materials")]
    public static void MenuGenerateTexturesAndMaterials()
    {
        SetupAllMaterials(true);
    }
#endif

    public static Material GetMaterial(string matId)
    {
        if (_materialCache.TryGetValue(matId, out Material mat) && mat != null)
        {
            return mat;
        }

#if UNITY_EDITOR
        string pathGen = $"{MATERIAL_DIR}/{matId}.mat";
        Material loaded = AssetDatabase.LoadAssetAtPath<Material>(pathGen);
        if (loaded != null)
        {
            _materialCache[matId] = loaded;
            return loaded;
        }
        string pathRoot = $"Assets/Materials/{matId}.mat";
        loaded = AssetDatabase.LoadAssetAtPath<Material>(pathRoot);
        if (loaded != null)
        {
            _materialCache[matId] = loaded;
            return loaded;
        }
#endif

        return SetupAllMaterials().TryGetValue(matId, out Material created) ? created : null;
    }

    /// <summary>
    /// Genera todos los mapas PBR y serializa/actualiza la biblioteca de materiales URP como archivos reales en disco.
    /// </summary>
    public static Dictionary<string, Material> SetupAllMaterials(bool forceRegenerate = false)
    {
#if UNITY_EDITOR
        EnsureFolders();
#endif
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") 
                     ?? Shader.Find("Universal Render Pipeline/Simple Lit") 
                     ?? Shader.Find("Universal Render Pipeline/Unlit");

        // Helper para crear o actualizar un material PBR persistente
        Material CreateOrUpdatePBRMat(
            string matId,
            Color baseColor,
            Texture2D baseTex,
            Texture2D normTex,
            Texture2D metalTex,
            Texture2D emitTex = null,
            Color? emitColor = null,
            float metallic = 1.0f,
            float smoothness = 1.0f,
            float bumpScale = 1.0f,
            Vector2? tiling = null)
        {
            Material mat = null;
#if UNITY_EDITOR
            string matPathGen = $"{MATERIAL_DIR}/{matId}.mat";
            string matPathRoot = $"Assets/Materials/{matId}.mat";
            mat = AssetDatabase.LoadAssetAtPath<Material>(matPathGen);
            if (mat == null)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPathRoot);
            }
            if (mat == null)
            {
                mat = new Material(urpLit);
                AssetDatabase.CreateAsset(mat, matPathGen);
            }
#else
            if (!_materialCache.TryGetValue(matId, out mat) || mat == null)
            {
                mat = new Material(urpLit);
            }
#endif
            mat.shader = urpLit;
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (baseTex != null)
            {
                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
                mat.mainTexture = baseTex;
            }

            if (normTex != null && mat.HasProperty("_BumpMap"))
            {
                mat.SetTexture("_BumpMap", normTex);
                mat.SetFloat("_BumpScale", bumpScale);
                mat.EnableKeyword("_NORMALMAP");
            }
            else
            {
                mat.DisableKeyword("_NORMALMAP");
            }

            if (metalTex != null && mat.HasProperty("_MetallicGlossMap"))
            {
                mat.SetTexture("_MetallicGlossMap", metalTex);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                mat.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (emitTex != null || emitColor.HasValue)
            {
                Color ec = emitColor ?? Color.white;
                if (emitTex != null && mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emitTex);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", ec);
                mat.EnableKeyword("_EMISSION");
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            if (tiling.HasValue)
            {
                mat.mainTextureScale = tiling.Value;
                if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling.Value);
                if (mat.HasProperty("_BumpMap")) mat.SetTextureScale("_BumpMap", tiling.Value);
                if (mat.HasProperty("_MetallicGlossMap")) mat.SetTextureScale("_MetallicGlossMap", tiling.Value);
            }

#if UNITY_EDITOR
            EditorUtility.SetDirty(mat);
            try
            {
                if (!File.Exists(Path.Combine(Application.dataPath, $"Materials/{matId}.mat")))
                {
                    AssetDatabase.CopyAsset(matPathGen, matPathRoot);
                }
                else
                {
                    Material rootMat = AssetDatabase.LoadAssetAtPath<Material>(matPathRoot);
                    if (rootMat != null && rootMat != mat)
                    {
                        rootMat.CopyPropertiesFromMaterial(mat);
                        EditorUtility.SetDirty(rootMat);
                    }
                }
            }
            catch { }
#endif
            _materialCache[matId] = mat;
            return mat;
        }

        // =====================================================================
        // 1. TEXTURAS DE INTERIOR (POR PLANTA)
        // =====================================================================

        // A. Planta 1: Rejilla Industrial y Chapa Estriada (Diamond Plate)
        var (dpBase, dpNorm, dpMetal) = GenerateDiamondPlate(256, 256);
        dpBase = SaveTex(dpBase, "Tex_DiamondPlate_Base");
        dpNorm = SaveTex(dpNorm, "Tex_DiamondPlate_Normal", true);
        dpMetal = SaveTex(dpMetal, "Tex_DiamondPlate_Metallic");

        CreateOrUpdatePBRMat("M_Floor1_DiamondPlate", Color.white, dpBase, dpNorm, dpMetal, null, null, 1.0f, 1.0f, 1.2f, new Vector2(6f, 6f));
        CreateOrUpdatePBRMat("M_CockpitFloor", new Color(0.25f, 0.27f, 0.30f), dpBase, dpNorm, dpMetal, null, null, 0.85f, 0.85f, 1.2f, new Vector2(6f, 6f));

        // B. Planta 1: Blindaje Militar Remachado (Muros Dieselpunk)
        var (rivBase, rivNorm, rivMetal) = GenerateRivetedArmor(256, 256);
        rivBase = SaveTex(rivBase, "Tex_RivetedArmor_Base");
        rivNorm = SaveTex(rivNorm, "Tex_RivetedArmor_Normal", true);
        rivMetal = SaveTex(rivMetal, "Tex_RivetedArmor_Metallic");

        CreateOrUpdatePBRMat("M_Floor1_RivetedArmor", Color.white, rivBase, rivNorm, rivMetal, null, null, 0.7f, 0.6f, 1.35f, new Vector2(4f, 2f));
        CreateOrUpdatePBRMat("M_CockpitWall", new Color(0.32f, 0.35f, 0.38f), rivBase, rivNorm, rivMetal, null, null, 0.65f, 0.55f, 1.2f, new Vector2(4f, 2f));

        // C. Planta 1: Núcleo de Reactor de Fusión Industrial
        var (reaBase, reaNorm, reaMetal, reaEmit) = GenerateFusionReactor(256, 256);
        reaBase = SaveTex(reaBase, "Tex_FusionReactor_Base");
        reaNorm = SaveTex(reaNorm, "Tex_FusionReactor_Normal", true);
        reaMetal = SaveTex(reaMetal, "Tex_FusionReactor_Metallic");
        reaEmit = SaveTex(reaEmit, "Tex_FusionReactor_Emission");

        CreateOrUpdatePBRMat("M_Floor1_ReactorCore", Color.white, reaBase, reaNorm, reaMetal, reaEmit, new Color(2.4f, 0.95f, 0.15f), 0.8f, 0.8f, 1.0f, new Vector2(2f, 2f));
        CreateOrUpdatePBRMat("M_MechaCoreOrange", new Color(1.0f, 0.45f, 0.05f), reaBase, reaNorm, reaMetal, reaEmit, new Color(2.4f, 0.95f, 0.15f), 0.8f, 0.85f, 1.0f, new Vector2(2f, 2f));

        // D. Planta 1 y 2: Pantallas CRT / Radar Militar
        var (crtBase, crtNorm, crtMetal, crtEmit) = GenerateCRTRadar(256, 256);
        crtBase = SaveTex(crtBase, "Tex_CRTRadar_Base");
        crtNorm = SaveTex(crtNorm, "Tex_CRTRadar_Normal", true);
        crtMetal = SaveTex(crtMetal, "Tex_CRTRadar_Metallic");
        crtEmit = SaveTex(crtEmit, "Tex_CRTRadar_Emission");

        CreateOrUpdatePBRMat("M_Floor1_CRTRadar", Color.white, crtBase, crtNorm, crtMetal, crtEmit, new Color(0.2f, 1.8f, 0.4f), 0.1f, 0.9f, 1.0f, Vector2.one);
        CreateOrUpdatePBRMat("M_BtnGreen", new Color(0.06f, 0.72f, 0.51f), crtBase, crtNorm, crtMetal, crtEmit, new Color(0.1f, 1.5f, 0.6f) * 1.5f, 0.2f, 0.8f, 1.0f, Vector2.one);

        // E. Franjas de Peligro (Hazard Stripes) para escaleras y bordes
        var (hzBase, hzNorm, hzMetal) = GenerateHazardStripes(256, 256);
        hzBase = SaveTex(hzBase, "Tex_HazardStripe_Base");
        hzNorm = SaveTex(hzNorm, "Tex_HazardStripe_Normal", true);
        hzMetal = SaveTex(hzMetal, "Tex_HazardStripe_Metallic");

        CreateOrUpdatePBRMat("M_HazardStripe", Color.white, hzBase, hzNorm, hzMetal, hzBase, new Color(0.6f, 0.45f, 0.05f), 0.35f, 0.65f, 1.0f, new Vector2(4f, 1f));

        // F. Planta 2: Cubierta de Portaaviones / Hangar Naval
        var (cdBase, cdNorm, cdMetal) = GenerateCarrierDeck(256, 256);
        cdBase = SaveTex(cdBase, "Tex_CarrierDeck_Base");
        cdNorm = SaveTex(cdNorm, "Tex_CarrierDeck_Normal", true);
        cdMetal = SaveTex(cdMetal, "Tex_CarrierDeck_Metallic");

        CreateOrUpdatePBRMat("M_Floor2_CarrierDeck", Color.white, cdBase, cdNorm, cdMetal, null, null, 0.65f, 0.5f, 1.3f, new Vector2(6f, 6f));

        // G. Planta 2: Tanques Criogénicos y Tuberías Escarchadas
        var (cryBase, cryNorm, cryMetal) = GenerateCryoFrost(256, 256);
        cryBase = SaveTex(cryBase, "Tex_CryoFrost_Base");
        cryNorm = SaveTex(cryNorm, "Tex_CryoFrost_Normal", true);
        cryMetal = SaveTex(cryMetal, "Tex_CryoFrost_Metallic");

        CreateOrUpdatePBRMat("M_Floor2_CryoFrost", Color.white, cryBase, cryNorm, cryMetal, null, null, 0.9f, 0.88f, 1.2f, new Vector2(2f, 4f));

        // H. Planta 1 y 2: Manómetro Analógico de Presión
        var (pgBase, pgNorm, pgMetal) = GeneratePressureGauge(256, 256);
        pgBase = SaveTex(pgBase, "Tex_PressureGauge_Base");
        pgNorm = SaveTex(pgNorm, "Tex_PressureGauge_Normal", true);
        pgMetal = SaveTex(pgMetal, "Tex_PressureGauge_Metallic");

        CreateOrUpdatePBRMat("M_Floor2_PressureGauge", Color.white, pgBase, pgNorm, pgMetal, null, null, 0.85f, 0.75f, 1.0f, Vector2.one);

        // I. Planta 2: Giróscopo de Puntería (AIM_PAD)
        var (aimBase, aimNorm, aimMetal, aimEmit) = GenerateAimPadDisc(256, 256);
        aimBase = SaveTex(aimBase, "Tex_AimPadDisc_Base");
        aimNorm = SaveTex(aimNorm, "Tex_AimPadDisc_Normal", true);
        aimMetal = SaveTex(aimMetal, "Tex_AimPadDisc_Metallic");
        aimEmit = SaveTex(aimEmit, "Tex_AimPadDisc_Emission");

        CreateOrUpdatePBRMat("M_Floor2_AimPad", Color.white, aimBase, aimNorm, aimMetal, aimEmit, new Color(0.0f, 1.6f, 1.8f), 0.75f, 0.75f, 1.2f, Vector2.one);

        // J. Planta 3: Fibra de Carbono Hexagonal y Circuitos Emisivos
        var (cfhBase, cfhNorm, cfhMetal, cfhEmit) = GenerateCarbonFiberHex(256, 256);
        cfhBase = SaveTex(cfhBase, "Tex_CarbonFiberHex_Base");
        cfhNorm = SaveTex(cfhNorm, "Tex_CarbonFiberHex_Normal", true);
        cfhMetal = SaveTex(cfhMetal, "Tex_CarbonFiberHex_Metallic");
        cfhEmit = SaveTex(cfhEmit, "Tex_CarbonFiberHex_Emission");

        CreateOrUpdatePBRMat("M_Floor3_CarbonHex", Color.white, cfhBase, cfhNorm, cfhMetal, cfhEmit, new Color(0.0f, 1.8f, 2.0f), 0.85f, 0.85f, 1.25f, new Vector2(8f, 8f));
        CreateOrUpdatePBRMat("M_Floor3_NeuralCircuit", Color.white, cfhBase, cfhNorm, cfhMetal, cfhEmit, new Color(0.0f, 2.2f, 2.4f), 0.9f, 0.9f, 1.0f, new Vector2(4f, 4f));

        // K. Planta 3: Racks de Supercomputadoras Neurales
        var (scBase, scNorm, scMetal, scEmit) = GenerateSupercomputer(256, 256);
        scBase = SaveTex(scBase, "Tex_Supercomputer_Base");
        scNorm = SaveTex(scNorm, "Tex_Supercomputer_Normal", true);
        scMetal = SaveTex(scMetal, "Tex_Supercomputer_Metallic");
        scEmit = SaveTex(scEmit, "Tex_Supercomputer_Emission");

        CreateOrUpdatePBRMat("M_Floor3_Supercomputer", Color.white, scBase, scNorm, scMetal, scEmit, new Color(0.0f, 1.8f, 2.0f), 0.8f, 0.75f, 1.1f, new Vector2(2f, 2f));

        // L. Planta 3: Anillo Holográfico de Mando del Capitán
        var (hdBase, hdNorm, hdMetal, hdEmit) = GenerateHoloDisc(256, 256);
        hdBase = SaveTex(hdBase, "Tex_HoloDisc_Base");
        hdNorm = SaveTex(hdNorm, "Tex_HoloDisc_Normal", true);
        hdMetal = SaveTex(hdMetal, "Tex_HoloDisc_Metallic");
        hdEmit = SaveTex(hdEmit, "Tex_HoloDisc_Emission");

        CreateOrUpdatePBRMat("M_Floor3_HoloDisc", Color.white, hdBase, hdNorm, hdMetal, hdEmit, new Color(0.0f, 2.0f, 2.2f), 0.85f, 0.9f, 1.0f, Vector2.one);
        CreateOrUpdatePBRMat("M_MechaCyanOptics", new Color(0.0f, 0.95f, 1.0f), hdBase, hdNorm, hdMetal, hdEmit, new Color(0.0f, 2.2f, 2.4f), 0.4f, 0.95f, 1.0f, Vector2.one);

        // =====================================================================
        // 2. TEXTURAS DEL EXTERIOR DEL MECHA (TITÁN DE 50M - ESTILO REAL ROBOT)
        // =====================================================================

        // A. Blanco Titanio Satinado / Gris Espacial Claro (Placas Principales de Blindaje)
        var (whiteBase, whiteNorm, whiteMetal) = GenerateMechaHullPlating(512, 512, new Color(0.92f, 0.93f, 0.96f), "MQ-01");
        whiteBase = SaveTex(whiteBase, "Tex_MechaArmorWhite_Base");
        whiteNorm = SaveTex(whiteNorm, "Tex_MechaArmorWhite_Normal", true);
        whiteMetal = SaveTex(whiteMetal, "Tex_MechaArmorWhite_Metallic");

        CreateOrUpdatePBRMat("M_MechaArmorWhite", Color.white, whiteBase, whiteNorm, whiteMetal, null, null, 0.70f, 0.72f, 1.35f, Vector2.one);
        CreateOrUpdatePBRMat("M_MechaHullPlating", Color.white, whiteBase, whiteNorm, whiteMetal, null, null, 0.72f, 0.70f, 1.35f, Vector2.one);

        // B. Azul Cobalto Oscuro (Bloque Central del Torso y Canillas)
        var (blueBase, blueNorm, blueMetal) = GenerateMechaHullPlating(512, 512, new Color(0.09f, 0.18f, 0.42f), "RX-78");
        blueBase = SaveTex(blueBase, "Tex_MechaCobaltBlue_Base");
        blueNorm = SaveTex(blueNorm, "Tex_MechaCobaltBlue_Normal", true);
        blueMetal = SaveTex(blueMetal, "Tex_MechaCobaltBlue_Metallic");

        CreateOrUpdatePBRMat("M_MechaCobaltBlue", Color.white, blueBase, blueNorm, blueMetal, null, null, 0.78f, 0.72f, 1.35f, Vector2.one);
        CreateOrUpdatePBRMat("M_MechaMilGreen", new Color(0.20f, 0.28f, 0.20f), whiteBase, whiteNorm, whiteMetal, null, null, 0.75f, 0.65f, 1.3f, Vector2.one);

        var (hullDarkBase, hullDarkNorm, hullDarkMetal) = GenerateMechaHullPlating(256, 256, new Color(0.06f, 0.10f, 0.24f), null);
        hullDarkBase = SaveTex(hullDarkBase, "Tex_MechaHullDark_Base");
        hullDarkNorm = SaveTex(hullDarkNorm, "Tex_MechaHullDark_Normal", true);
        hullDarkMetal = SaveTex(hullDarkMetal, "Tex_MechaHullDark_Metallic");
        CreateOrUpdatePBRMat("M_MechaMilGreenDark", new Color(0.06f, 0.10f, 0.24f), hullDarkBase, hullDarkNorm, hullDarkMetal, null, null, 0.75f, 0.6f, 1.3f, new Vector2(3f, 3f));

        // C. Rojo Carmesí (Acentos, Barbilla y Ribetes - Esmalte Metálico Liso sin ladrillos/tejas)
        var (redBase, redNorm, redMetal) = GenerateSmoothMechaArmor(256, 256, new Color(0.85f, 0.08f, 0.12f), 0.55f, 0.82f);
        redBase = SaveTex(redBase, "Tex_MechaCrimsonRed_Base");
        redNorm = SaveTex(redNorm, "Tex_MechaCrimsonRed_Normal", true);
        redMetal = SaveTex(redMetal, "Tex_MechaCrimsonRed_Metallic");
        CreateOrUpdatePBRMat("M_MechaCrimsonRed", Color.white, redBase, redNorm, redMetal, null, null, 0.55f, 0.82f, 1.0f, Vector2.one);

        // D. Tomas de Ventilación del Pecho (Chest Vents - Amarillo Hazard / Naranja Cálido)
        var (ventBase, ventNorm, ventMetal) = GenerateMechaHullPlating(256, 256, new Color(0.98f, 0.72f, 0.08f), null);
        ventBase = SaveTex(ventBase, "Tex_MechaChestVent_Base");
        ventNorm = SaveTex(ventNorm, "Tex_MechaChestVent_Normal", true);
        ventMetal = SaveTex(ventMetal, "Tex_MechaChestVent_Metallic");
        CreateOrUpdatePBRMat("M_MechaChestVent", Color.white, ventBase, ventNorm, ventMetal, null, null, 0.60f, 0.75f, 1.1f, new Vector2(2f, 2f));
        CreateOrUpdatePBRMat("M_MechaHazardYellow", Color.white, ventBase, ventNorm, ventMetal, null, null, 0.60f, 0.75f, 1.1f, new Vector2(2f, 2f));

        // E. Hidráulica, Bastidor Interno y Articulaciones (Inner Frame de Acero Grafito Oscuro)
        var (hydBase, hydNorm, hydMetal) = GenerateHydraulicSteel(256, 256);
        hydBase = SaveTex(hydBase, "Tex_HydraulicSteel_Base");
        hydNorm = SaveTex(hydNorm, "Tex_HydraulicSteel_Normal", true);
        hydMetal = SaveTex(hydMetal, "Tex_HydraulicSteel_Metallic");

        CreateOrUpdatePBRMat("M_MechaInnerFrame", new Color(0.12f, 0.13f, 0.16f), hydBase, hydNorm, hydMetal, null, null, 0.92f, 0.65f, 1.25f, new Vector2(3f, 3f));
        CreateOrUpdatePBRMat("M_MechaHydraulics", Color.white, hydBase, hydNorm, hydMetal, null, null, 0.95f, 0.88f, 1.0f, new Vector2(2f, 4f));
        CreateOrUpdatePBRMat("M_MechaChrome", new Color(0.90f, 0.92f, 0.96f), hydBase, hydNorm, hydMetal, null, null, 0.96f, 0.92f, 0.9f, new Vector2(2f, 2f));
        CreateOrUpdatePBRMat("M_MechaDarkSteel", new Color(0.10f, 0.11f, 0.13f), hydBase, hydNorm, hydMetal, null, null, 0.85f, 0.55f, 1.2f, new Vector2(3f, 3f));
        CreateOrUpdatePBRMat("M_MechaGunmetal", new Color(0.16f, 0.18f, 0.21f), hydBase, hydNorm, hydMetal, null, null, 0.82f, 0.65f, 1.1f, new Vector2(3f, 3f));
        CreateOrUpdatePBRMat("M_MechaBrass", new Color(0.78f, 0.62f, 0.25f), hydBase, hydNorm, hydMetal, null, null, 0.88f, 0.75f, 1.0f, new Vector2(2f, 2f));
        CreateOrUpdatePBRMat("M_MechaGoldVFin", new Color(0.96f, 0.82f, 0.22f), hydBase, hydNorm, hydMetal, null, null, 0.95f, 0.85f, 1.0f, new Vector2(2f, 2f));

        // F. Arsenal Exterior (Cañones Pavonados con Heat Tint y Carcasa Táctica)
        var (barBase, barNorm, barMetal) = GenerateArsenalBarrel(256, 256);
        barBase = SaveTex(barBase, "Tex_ArsenalBarrel_Base");
        barNorm = SaveTex(barNorm, "Tex_ArsenalBarrel_Normal", true);
        barMetal = SaveTex(barMetal, "Tex_ArsenalBarrel_Metallic");

        CreateOrUpdatePBRMat("M_MechaArsenalBarrels", Color.white, barBase, barNorm, barMetal, null, null, 0.95f, 0.82f, 1.2f, new Vector2(1f, 3f));

        // G. Botas Hazard Stomper
        var (bootBase, bootNorm, bootMetal) = GenerateHazardStomper(256, 256);
        bootBase = SaveTex(bootBase, "Tex_HazardStomper_Base");
        bootNorm = SaveTex(bootNorm, "Tex_HazardStomper_Normal", true);
        bootMetal = SaveTex(bootMetal, "Tex_HazardStomper_Metallic");

        CreateOrUpdatePBRMat("M_MechaHazardStomper", Color.white, bootBase, bootNorm, bootMetal, null, null, 0.8f, 0.65f, 1.3f, new Vector2(3f, 3f));

        // Materiales utilitarios
        CreateOrUpdatePBRMat("M_MechaDecalWhite", Color.white, null, null, null, null, Color.white, 0.1f, 0.9f);
        CreateOrUpdatePBRMat("M_BtnBase", new Color(0.15f, 0.16f, 0.18f), hydBase, hydNorm, hydMetal, null, null, 0.8f, 0.5f);
        CreateOrUpdatePBRMat("M_BtnAmber", new Color(0.96f, 0.62f, 0.04f), crtBase, crtNorm, crtMetal, crtEmit, new Color(1.8f, 1.1f, 0.1f), 0.2f, 0.8f);
        CreateOrUpdatePBRMat("M_BtnCyan", new Color(0.02f, 0.82f, 0.95f), crtBase, crtNorm, crtMetal, crtEmit, new Color(0.1f, 1.6f, 1.9f), 0.2f, 0.8f);
        CreateOrUpdatePBRMat("M_BtnRose", new Color(0.95f, 0.25f, 0.37f), crtBase, crtNorm, crtMetal, crtEmit, new Color(1.9f, 0.3f, 0.5f), 0.2f, 0.8f);

        // Cristal de la Cúpula Panorámica (URP Transparent)
        Material matDomeGlass = GetMaterial("M_CockpitDomeGlass");
        if (matDomeGlass == null)
        {
            matDomeGlass = new Material(urpLit);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(matDomeGlass, $"{MATERIAL_DIR}/M_CockpitDomeGlass.mat");
#endif
        }
        matDomeGlass.shader = urpLit;
        matDomeGlass.SetFloat("_Surface", 1.0f); // Transparent
        matDomeGlass.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        matDomeGlass.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        matDomeGlass.SetInt("_ZWrite", 0);
        matDomeGlass.renderQueue = 3000;
        matDomeGlass.color = new Color(0.6f, 0.85f, 0.95f, 0.08f);
        if (matDomeGlass.HasProperty("_BaseColor")) matDomeGlass.SetColor("_BaseColor", new Color(0.6f, 0.85f, 0.95f, 0.08f));
        if (matDomeGlass.HasProperty("_Smoothness")) matDomeGlass.SetFloat("_Smoothness", 0.95f);
        if (matDomeGlass.HasProperty("_Metallic")) matDomeGlass.SetFloat("_Metallic", 0.1f);
#if UNITY_EDITOR
        EditorUtility.SetDirty(matDomeGlass);
        try
        {
            string rootPath = "Assets/Materials/M_CockpitDomeGlass.mat";
            if (!File.Exists(Path.Combine(Application.dataPath, "Materials/M_CockpitDomeGlass.mat")))
                AssetDatabase.CopyAsset($"{MATERIAL_DIR}/M_CockpitDomeGlass.mat", rootPath);
            else
            {
                Material rm = AssetDatabase.LoadAssetAtPath<Material>(rootPath);
                if (rm != null && rm != matDomeGlass) { rm.CopyPropertiesFromMaterial(matDomeGlass); EditorUtility.SetDirty(rm); }
            }
        }
        catch { }
#endif
        _materialCache["M_CockpitDomeGlass"] = matDomeGlass;

        // =====================================================================
        // 3. TEXTURAS DEL PATO PILOTO
        // =====================================================================

        // A. Goma Sintética Texturizada (Rubber Duck PBR)
        var (duckBase, duckNorm, duckMetal) = GenerateRubberDuck(256, 256);
        duckBase = SaveTex(duckBase, "Tex_RubberDuck_Base");
        duckNorm = SaveTex(duckNorm, "Tex_RubberDuck_Normal", true);
        duckMetal = SaveTex(duckMetal, "Tex_RubberDuck_Metallic");

        CreateOrUpdatePBRMat("M_DuckRubberPBR", Color.white, duckBase, duckNorm, duckMetal, null, null, 0.02f, 0.65f, 0.8f, new Vector2(2f, 2f));
        CreateOrUpdatePBRMat("M_DuckYellow", new Color(1.0f, 0.82f, 0.05f), duckBase, duckNorm, duckMetal, null, null, 0.02f, 0.65f, 0.8f, new Vector2(2f, 2f));

        // B. Pico Naranja Satinado
        var (beakBase, beakNorm, beakMetal) = GenerateDuckBeak(128, 128);
        beakBase = SaveTex(beakBase, "Tex_DuckBeak_Base");
        beakNorm = SaveTex(beakNorm, "Tex_DuckBeak_Normal", true);
        beakMetal = SaveTex(beakMetal, "Tex_DuckBeak_Metallic");

        CreateOrUpdatePBRMat("M_DuckBeakPBR", Color.white, beakBase, beakNorm, beakMetal, null, null, 0.02f, 0.55f, 0.7f, Vector2.one);
        CreateOrUpdatePBRMat("M_DuckOrange", new Color(1.0f, 0.44f, 0.02f), beakBase, beakNorm, beakMetal, null, null, 0.02f, 0.55f, 0.7f, Vector2.one);

        // C. Casco y Accesorios Tácticos
        var (tacBase, tacNorm, tacMetal, tacEmit) = GenerateDuckTactical(128, 128);
        tacBase = SaveTex(tacBase, "Tex_DuckTactical_Base");
        tacNorm = SaveTex(tacNorm, "Tex_DuckTactical_Normal", true);
        tacMetal = SaveTex(tacMetal, "Tex_DuckTactical_Metallic");
        tacEmit = SaveTex(tacEmit, "Tex_DuckTactical_Emission");

        CreateOrUpdatePBRMat("M_DuckTacticalPBR", Color.white, tacBase, tacNorm, tacMetal, tacEmit, new Color(0.0f, 1.8f, 2.0f), 0.7f, 0.65f, 1.0f, Vector2.one);
        CreateOrUpdatePBRMat("M_DuckBlack", new Color(0.08f, 0.08f, 0.09f), tacBase, tacNorm, tacMetal, null, null, 0.35f, 0.75f, 0.8f, Vector2.one);

        // D. Botones con Iconos
        CreateButtonIconMat("FWD", "▲", new Color(0.06f, 0.85f, 0.51f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("BWD", "▼", new Color(0.96f, 0.62f, 0.04f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("LEFT", "◄", new Color(0.02f, 0.82f, 0.95f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("RIGHT", "►", new Color(0.02f, 0.82f, 0.95f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("GATLING", "🔫", new Color(0.96f, 0.62f, 0.04f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("MISSILES", "🚀", new Color(0.02f, 0.82f, 0.95f), CreateOrUpdatePBRMat);
        CreateButtonIconMat("SPAWN", "🚨", new Color(0.95f, 0.25f, 0.37f), CreateOrUpdatePBRMat);

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
        Debug.Log("🎨 ¡Motor de Texturizado Procedural PBR ha generado y guardado con éxito todas las texturas (.png) y materiales (.mat) en disco!");
        return _materialCache;
    }

    private delegate Material PBRMatCreator(string matId, Color baseColor, Texture2D baseTex, Texture2D normTex, Texture2D metalTex, Texture2D emitTex, Color? emitColor, float metallic, float smoothness, float bumpScale, Vector2? tiling);

    private static void CreateButtonIconMat(
        string btnId,
        string iconSymbol,
        Color color,
        PBRMatCreator creator)
    {
        var (bBase, bNorm, bMetal, bEmit) = GenerateButtonIcon(128, 128, color, iconSymbol);
        bBase = SaveTex(bBase, $"Tex_Btn_{btnId}_Base");
        bNorm = SaveTex(bNorm, $"Tex_Btn_{btnId}_Normal", true);
        bMetal = SaveTex(bMetal, $"Tex_Btn_{btnId}_Metallic");
        bEmit = SaveTex(bEmit, $"Tex_Btn_{btnId}_Emission");

        creator($"M_Btn_{btnId}", Color.white, bBase, bNorm, bMetal, bEmit, color * 1.6f, 0.4f, 0.8f, 1.0f, Vector2.one);
    }

    private static Texture2D SaveTex(Texture2D tex, string name, bool isNormal = false)
    {
        if (tex == null) return null;
#if UNITY_EDITOR
        try
        {
            string relPath = $"{TEXTURE_DIR}/{name}.png";
            string fullPath = Path.Combine(Application.dataPath, relPath.Replace("Assets/", "")).Replace("\\", "/");
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            byte[] bytes = ImageConversion.EncodeToPNG(tex);
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceUpdate);

            TextureImporter imp = AssetImporter.GetAtPath(relPath) as TextureImporter;
            if (imp != null)
            {
                if (isNormal)
                {
                    imp.textureType = TextureImporterType.NormalMap;
                }
                else
                {
                    imp.textureType = TextureImporterType.Default;
                }
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
            Texture2D diskTex = AssetDatabase.LoadAssetAtPath<Texture2D>(relPath);
            if (diskTex != null)
            {
                _textureCache[name] = diskTex;
                return diskTex;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[TEXTURE GENERATOR] No se pudo guardar {name} en disco: {ex.Message}");
        }
#endif
        _textureCache[name] = tex;
        return tex;
    }

#if UNITY_EDITOR
    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            AssetDatabase.CreateFolder("Assets", "Textures");
        if (!AssetDatabase.IsValidFolder("Assets/Textures/Generated"))
            AssetDatabase.CreateFolder("Assets/Textures", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Generated"))
            AssetDatabase.CreateFolder("Assets/Materials", "Generated");
    }
#endif

    // =====================================================================
    // SÍNTESIS MATEMÁTICA DE MAPAS PROCEDURALES (ZERO EXTERNAL ASSETS)
    // =====================================================================

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateDiamondPlate(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        float tileSize = w / 8f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = (x % tileSize) / tileSize;
                float v = (y % tileSize) / tileSize;
                int tileX = (int)(x / tileSize);
                int tileY = (int)(y / tileSize);
                bool alt = (tileX + tileY) % 2 == 0;

                // Dos diamantes diagonales perpendiculares por baldosa
                float d1 = MathDiamondDist(u - 0.5f, v - 0.5f, alt ? 0.35f : -0.35f);
                float diamondHeight = Mathf.Clamp01(1.0f - d1 * 3.2f);
                float height = Mathf.SmoothStep(0.05f, 0.85f, diamondHeight);

                // Micro-rugosidad de acero y arañazos
                float noise = Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 0.12f;
                height = Mathf.Clamp01(height + noise);
                heights[x, y] = height;

                // Color: Acero desgastado en crestas, grasa/óxido en valles
                byte baseShade = (byte)Mathf.Clamp(55 + height * 125, 0, 255);
                byte r = baseShade;
                byte g = (byte)(baseShade * 0.98f);
                byte b = (byte)(baseShade * 1.04f);

                // En valles, toque de óxido / grasa
                if (height < 0.25f)
                {
                    r = (byte)Mathf.Clamp(r + 14, 0, 255);
                    g = (byte)Mathf.Clamp(g - 6, 0, 255);
                    b = (byte)Mathf.Clamp(b - 12, 0, 255);
                }

                basePix[y * w + x] = new Color32(r, g, b, 255);

                // Metallic: R=Metal (0.85 en crestas), G=AO, A=Smoothness (0.8 en crestas pulidas por pisadas)
                byte metallic = (byte)(height > 0.4f ? 230 : 160);
                byte ao = (byte)Mathf.Clamp(130 + height * 125, 0, 255);
                byte smoothness = (byte)Mathf.Clamp(70 + height * 145, 0, 255);
                metalPix[y * w + x] = new Color32(metallic, ao, 0, smoothness);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.4f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateRivetedArmor(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        int panelSize = w / 2;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int px = x % panelSize;
                int py = y % panelSize;
                int borderDistX = Mathf.Min(px, panelSize - px);
                int borderDistY = Mathf.Min(py, panelSize - py);
                int edgeDist = Mathf.Min(borderDistX, borderDistY);

                float height = 0.65f;
                bool isSeam = edgeDist <= 4;
                if (isSeam)
                {
                    height = 0.05f; // Ranura profunda entre paneles
                }

                // Filas de remaches perimetrales a 14px del borde
                bool isRivet = false;
                if ((borderDistX >= 10 && borderDistX <= 18) || (borderDistY >= 10 && borderDistY <= 18))
                {
                    int stepX = (px - 14) % 24;
                    int stepY = (py - 14) % 24;
                    float rx = ((borderDistX >= 10 && borderDistX <= 18) ? borderDistX - 14 : stepX - 12);
                    float ry = ((borderDistY >= 10 && borderDistY <= 18) ? borderDistY - 14 : stepY - 12);
                    float distFromRivet = Mathf.Sqrt(rx * rx + ry * ry);
                    if (distFromRivet < 5.0f)
                    {
                        isRivet = true;
                        float dome = Mathf.Cos(distFromRivet / 5.0f * Mathf.PI * 0.5f);
                        height = 0.65f + dome * 0.35f;
                    }
                }

                float noise = Mathf.PerlinNoise(x * 0.06f, y * 0.06f);
                heights[x, y] = Mathf.Clamp01(height + noise * 0.08f);

                // Verde militar oliva con desconchones plateados en bordes
                Color c = new Color(0.20f, 0.28f, 0.20f);
                byte metallic = 175;
                byte smoothness = 110;

                if (isSeam)
                {
                    c = new Color(0.08f, 0.09f, 0.08f);
                    smoothness = 60;
                }
                else if (edgeDist <= 8 || isRivet)
                {
                    // Desconchón metálico plateado por desgaste
                    c = Color.Lerp(c, new Color(0.68f, 0.72f, 0.75f), 0.75f);
                    metallic = 230;
                    smoothness = 180;
                }
                else
                {
                    // Manchas de grasa y óxido procedimentales
                    float grime = Mathf.PerlinNoise(x * 0.12f + 50f, y * 0.12f + 50f);
                    if (grime > 0.65f)
                    {
                        c = Color.Lerp(c, new Color(0.35f, 0.20f, 0.10f), (grime - 0.65f) * 2.5f);
                        smoothness = 80;
                    }
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(metallic, (byte)(height * 255), 0, smoothness);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.8f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateHazardStripes(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        float stripeWidth = w / 8f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float diag = (x + y) / stripeWidth;
                bool isYellow = (Mathf.FloorToInt(diag) % 2) == 0;

                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.2f;
                float height = isYellow ? 0.62f : 0.55f; // Ligero relieve de la pintura
                heights[x, y] = height + noise * 0.1f;

                Color c = isYellow ? new Color(0.96f, 0.72f, 0.05f) : new Color(0.12f, 0.12f, 0.14f);

                // Desgaste en los bordes de franja
                float stripeFrac = Mathf.Abs((diag % 1.0f) - 0.5f);
                if (stripeFrac > 0.44f && noise > 0.12f)
                {
                    c = Color.Lerp(c, new Color(0.55f, 0.58f, 0.60f), 0.7f); // Acero expuesto
                }

                basePix[y * w + x] = c;
                byte metallic = (byte)(isYellow ? 90 : 180);
                byte smoothness = (byte)(isYellow ? 150 : 100);
                metalPix[y * w + x] = new Color32(metallic, 230, 0, smoothness);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.8f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateCarbonFiberHex(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float hexRadius = w / 12f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                var (hexDist, isBorder) = CalcHexGrid(x, y, hexRadius);
                float twill = ((x / 4) % 2 == (y / 4) % 2) ? 0.08f : -0.08f;
                float height = Mathf.Clamp01(0.7f - hexDist * 0.35f + twill);
                heights[x, y] = height;

                Color c = new Color(0.07f, 0.08f, 0.09f);
                Color ec = Color.black;

                // Trazas de circuitos emisivos cian en aristas seleccionadas
                if (isBorder && (x * 7 + y * 13) % 29 < 7)
                {
                    ec = new Color(0.0f, 0.95f, 1.0f);
                    c = Color.Lerp(c, ec, 0.85f);
                }

                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(210, 240, 0, (byte)(180 + twill * 300));
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.2f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateCRTRadar(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;
        float maxR = w * 0.45f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                // Fondo verde fósforo oscuro militar
                Color c = new Color(0.02f, 0.08f, 0.04f);
                Color ec = Color.black;
                float height = 0.5f;

                // Anillos de rango de radar concéntricos
                float ringDist = (r / maxR) * 4f;
                bool isRing = Mathf.Abs(ringDist - Mathf.Round(ringDist)) < 0.06f && r <= maxR;

                // Ejes cardinales (Cruz)
                bool isAxis = (Mathf.Abs(dx) < 1.5f || Mathf.Abs(dy) < 1.5f) && r <= maxR;

                // Rayo barredor de radar (Sweep Beam)
                float sweepAngle = Mathf.Repeat(angle + 45f, 360f);
                bool isSweep = sweepAngle < 40f && r <= maxR;

                // Puntos de contacto hostil (Blips)
                bool isBlip = (Mathf.Abs(dx - 35) < 3 && Mathf.Abs(dy - 40) < 3) ||
                              (Mathf.Abs(dx + 50) < 4 && Mathf.Abs(dy - 20) < 4) ||
                              (Mathf.Abs(dx - 15) < 3 && Mathf.Abs(dy + 60) < 3);

                // Scanlines CRT horizontales
                bool isScanline = (y % 4) == 0;

                if (isBlip)
                {
                    ec = new Color(1.0f, 0.25f, 0.15f); // Blip hostil rojo
                    c = ec;
                }
                else if (isRing || isAxis)
                {
                    ec = new Color(0.15f, 0.95f, 0.35f); // Retícula verde
                    c = ec;
                }
                else if (isSweep)
                {
                    float fade = 1.0f - (sweepAngle / 40f);
                    ec = new Color(0.05f, 0.55f * fade, 0.15f * fade);
                    c = Color.Lerp(c, ec, 0.7f);
                }

                if (isScanline)
                {
                    c *= 0.75f;
                    ec *= 0.8f;
                }

                // Curvatura del cristal del monitor en bordes
                float vign = Mathf.Clamp01(1.0f - (r / (w * 0.65f)));
                c *= vign;
                ec *= vign;

                heights[x, y] = height;
                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(40, 255, 0, 230); // Cristal suave muy reflectante
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.0f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateCarrierDeck(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Agregado rugoso antideslizante de cubierta de vuelo
                float noise = Mathf.PerlinNoise(x * 0.35f, y * 0.35f) * 0.4f;
                float height = 0.5f + noise;
                heights[x, y] = height;

                Color c = new Color(0.18f, 0.20f, 0.22f); // Gris naval

                // Líneas de aparcamiento y señalización de catapulta en amarillo/blanco
                if (x >= w * 0.48f && x <= w * 0.52f)
                {
                    c = new Color(0.92f, 0.75f, 0.08f); // Línea central amarilla
                }
                else if ((x % 64 < 4) || (y % 64 < 4))
                {
                    c = new Color(0.13f, 0.14f, 0.16f); // Juntas de planchas de cubierta
                }

                // Puntos de anclaje (Tie-down cups) en cruz
                int cupX = x % 64 - 32;
                int cupY = y % 64 - 32;
                if (cupX * cupX + cupY * cupY < 36)
                {
                    c = new Color(0.08f, 0.09f, 0.10f);
                    height = 0.1f;
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(180, (byte)(height * 255), 0, 110);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.5f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateFusionReactor(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (w * 0.5f);
                float angle = Mathf.Atan2(dy, dx);

                Color c = new Color(0.12f, 0.13f, 0.15f);
                Color ec = Color.black;
                float height = 0.5f;

                // Anillos magnéticos de contención plasmática
                float ring = Mathf.Sin(dist * Mathf.PI * 8f);
                if (ring > 0.4f && dist < 0.9f)
                {
                    float p = (ring - 0.4f) / 0.6f;
                    ec = Color.Lerp(new Color(1.0f, 0.25f, 0.0f), new Color(1.0f, 0.85f, 0.2f), p);
                    c = ec;
                    height = 0.8f;
                }
                else
                {
                    // Aletas radiales de disipación
                    float fin = Mathf.Sin(angle * 16f);
                    if (fin > 0.6f) height = 0.7f;
                }

                heights[x, y] = height;
                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(230, 220, 0, 190);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.6f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateCryoFrost(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Acero cromado base con estrías verticales
                float metalStr = (x % 3 == 0) ? 0.06f : -0.06f;
                // Cristales de escarcha criogénica
                float frostNoise = Mathf.PerlinNoise(x * 0.08f, y * 0.08f) + Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.5f;

                float height = 0.5f + metalStr + (frostNoise > 0.8f ? 0.35f : 0f);
                heights[x, y] = height;

                Color c = new Color(0.82f, 0.86f, 0.90f); // Acero inoxidable pulido
                byte smoothness = 235;

                if (frostNoise > 0.8f)
                {
                    // Escarcha azul hielo
                    c = Color.Lerp(c, new Color(0.75f, 0.94f, 1.0f), (frostNoise - 0.8f) * 4f);
                    smoothness = 120; // Más mate en la escarcha
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(245, 255, 0, smoothness);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.6f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GeneratePressureGauge(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (w * 0.5f);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                Color c = Color.white;
                byte metallic = 40;
                byte smoothness = 210;
                float height = 0.5f;

                if (dist > 0.85f && dist < 1.0f)
                {
                    // Bisel exterior de latón
                    c = new Color(0.82f, 0.65f, 0.28f);
                    metallic = 230;
                    smoothness = 190;
                    height = 0.9f;
                }
                else if (dist <= 0.85f)
                {
                    // Esfera blanca esmaltada con marcas de graduación
                    c = new Color(0.92f, 0.93f, 0.95f);
                    if (dist > 0.68f && dist < 0.80f)
                    {
                        float tick = Mathf.Abs(Mathf.Repeat(angle + 180f, 15f) - 7.5f);
                        if (tick < 1.0f)
                        {
                            c = Color.black; // Marcas de escala
                        }
                    }

                    // Arco de advertencia rojo
                    if (dist > 0.60f && dist < 0.68f && angle > 30f && angle < 110f)
                    {
                        c = new Color(0.85f, 0.15f, 0.15f);
                    }

                    // Aguja indicadora roja
                    float needleAng = 45f;
                    float dAng = Mathf.DeltaAngle(angle, needleAng);
                    if (Mathf.Abs(dAng) < 3.0f && dist < 0.72f)
                    {
                        c = new Color(0.95f, 0.1f, 0.1f);
                        height = 0.7f;
                    }

                    // Núcleo central
                    if (dist < 0.12f)
                    {
                        c = new Color(0.82f, 0.65f, 0.28f);
                        metallic = 230;
                        height = 0.85f;
                    }
                }

                heights[x, y] = height;
                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(metallic, 240, 0, smoothness);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.8f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateAimPadDisc(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (w * 0.5f);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                Color c = new Color(0.12f, 0.14f, 0.16f); // Acero militar oscuro
                Color ec = Color.black;
                float height = 0.5f;

                // Anillo de rumbo azimutal graduado
                if (dist > 0.82f && dist < 0.94f)
                {
                    float degTick = Mathf.Abs(Mathf.Repeat(angle + 180f, 10f) - 5f);
                    if (degTick < 1.0f)
                    {
                        ec = new Color(0.0f, 0.95f, 1.0f);
                        c = ec;
                    }
                    else
                    {
                        c = new Color(0.24f, 0.26f, 0.30f);
                    }
                    height = 0.7f;
                }

                // Cruz cardinal táctica
                if ((Mathf.Abs(dx) < 2f || Mathf.Abs(dy) < 2f) && dist < 0.8f && dist > 0.25f)
                {
                    ec = new Color(0.0f, 0.85f, 1.0f);
                    c = ec;
                    height = 0.65f;
                }

                // Lente central Lock-On
                if (dist <= 0.25f)
                {
                    float p = 1.0f - dist / 0.25f;
                    ec = new Color(0.0f, 1.2f * p, 1.5f * p);
                    c = ec;
                    height = 0.85f;
                }

                heights[x, y] = height;
                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(220, 240, 0, 180);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.4f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateSmoothMechaArmor(int w, int h, Color baseColor, float metallic = 0.5f, float smoothness = 0.75f)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        byte mByte = (byte)(Mathf.Clamp01(metallic) * 255);
        byte sByte = (byte)(Mathf.Clamp01(smoothness) * 255);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Micro-rugosidad sutil satinada (cero patrones de ladrillo o cuadrículas repetitivas)
                float n = (Mathf.PerlinNoise(x * 0.15f, y * 0.15f) - 0.5f) * 0.03f;
                float hVal = Mathf.Clamp01(0.5f + n);
                heights[x, y] = hVal;

                Color c = Color.Lerp(baseColor * 0.97f, baseColor * 1.03f, n * 15f + 0.5f);
                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(mByte, (byte)(hVal * 255), 0, sByte);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.0f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateMechaHullPlating(int w, int h, Color baseColor, string stencil)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        // Placa monolítica amplia (sin subdivisión en mini-ladrillos)
        int pW = w;
        int pH = h;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int lx = x % pW;
                int ly = y % pH;
                int bDist = Mathf.Min(Mathf.Min(lx, pW - lx), Mathf.Min(ly, pH - ly));

                float height = 0.65f;
                bool isSeam = bDist <= 3;
                if (isSeam)
                {
                    height = 0.05f; // Línea de panel profunda
                }

                // Pernos en esquinas de panel
                bool isBolt = (lx <= 8 || lx >= pW - 8) && (ly <= 8 || ly >= pH - 8);
                if (isBolt)
                {
                    int bx = lx <= 8 ? lx - 4 : lx - (pW - 4);
                    int by = ly <= 8 ? ly - 4 : ly - (pH - 4);
                    if (bx * bx + by * by <= 9)
                    {
                        height = 0.95f;
                    }
                }

                float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                heights[x, y] = Mathf.Clamp01(height + noise * 0.06f);

                Color c = baseColor;
                byte metallic = 170;
                byte smoothness = 120;

                if (isSeam)
                {
                    c = new Color(0.06f, 0.07f, 0.06f);
                    smoothness = 60;
                }
                else if (bDist <= 6)
                {
                    // Desconchón de titanio pulido en bordes
                    c = Color.Lerp(c, new Color(0.75f, 0.78f, 0.82f), 0.8f);
                    metallic = 240;
                    smoothness = 195;
                }

                // Manchas de quemaduras de combate
                float scorch = Mathf.PerlinNoise(x * 0.08f + 120f, y * 0.08f + 80f);
                if (scorch > 0.72f)
                {
                    c = Color.Lerp(c, new Color(0.08f, 0.08f, 0.09f), (scorch - 0.72f) * 3f);
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(metallic, (byte)(height * 255), 0, smoothness);
            }
        }

        // Estarcido táctico militar si se especifica
        if (!string.IsNullOrEmpty(stencil))
        {
            ApplyStencilDecal(basePix, w, h, stencil);
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 3.0f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateHydraulicSteel(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Estrías longitudinales micrométricas de torneado
                float groove = ((x % 2 == 0) ? 0.03f : -0.03f);
                float height = 0.7f + groove;
                heights[x, y] = height;

                Color c = new Color(0.85f, 0.88f, 0.92f); // Acero bruñido

                // Anillo de retén con aceite lubricante oscuro cada cierto tramo
                if (y % 128 < 6)
                {
                    c = new Color(0.12f, 0.11f, 0.09f); // Grasa hidráulica
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(245, 255, 0, 225);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.5f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateArsenalBarrel(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            float tipProgress = (float)y / h; // 1.0 = punta de la boca
            for (int x = 0; x < w; x++)
            {
                // Estrías de refrigeración y acanalado
                float fluting = Mathf.Sin((float)x / w * Mathf.PI * 16f) * 0.15f;
                float height = 0.65f + fluting;
                heights[x, y] = height;

                // Pavonado gunmetal base
                Color c = new Color(0.18f, 0.19f, 0.22f);

                // Discoloración térmica rainbow heat tint en la boca del cañón
                if (tipProgress > 0.65f)
                {
                    float hp = (tipProgress - 0.65f) / 0.35f;
                    if (hp < 0.33f)
                        c = Color.Lerp(c, new Color(0.75f, 0.45f, 0.15f), hp * 3f); // Bronce recalentado
                    else if (hp < 0.66f)
                        c = Color.Lerp(new Color(0.75f, 0.45f, 0.15f), new Color(0.55f, 0.15f, 0.65f), (hp - 0.33f) * 3f); // Púrpura
                    else
                        c = Color.Lerp(new Color(0.55f, 0.15f, 0.65f), new Color(0.15f, 0.35f, 0.85f), (hp - 0.66f) * 3f); // Azul cobalto
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(235, 240, 0, 210);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.0f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateHazardStomper(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Tacos de suela masiva
                bool lug = (x % 32 < 24) && (y % 32 < 24);
                float height = lug ? 0.85f : 0.2f;
                heights[x, y] = height;

                Color c = new Color(0.15f, 0.16f, 0.18f); // Hierro fundido
                if (x % 48 < 24 && y > h * 0.7f)
                {
                    c = new Color(0.95f, 0.72f, 0.05f); // Franja amarilla en puntera
                }

                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(210, (byte)(height * 255), 0, 110);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 3.2f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateRubberDuck(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Micro-rugosidad porosa sintética de goma de patito
                float pore = Mathf.PerlinNoise(x * 0.45f, y * 0.45f) * 0.08f;
                // Costura sutil de molde de inyección en línea media
                float moldSeam = (x == w / 2) ? 0.06f : 0f;

                float height = 0.5f + pore + moldSeam;
                heights[x, y] = height;

                // Amarillo patito alegre y vibrante
                Color c = new Color(1.0f, 0.82f, 0.06f);

                basePix[y * w + x] = c;
                // Goma: No metálica (0.02), alta oclusión, smoothness medio-alto plástico (0.65)
                metalPix[y * w + x] = new Color32(5, 250, 0, 165);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.4f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap) GenerateDuckBeak(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float grain = (y % 4 == 0) ? 0.04f : -0.04f;
                float height = 0.5f + grain;
                heights[x, y] = height;

                Color c = new Color(1.0f, 0.45f, 0.02f); // Naranja pico
                basePix[y * w + x] = c;
                metalPix[y * w + x] = new Color32(5, 250, 0, 140);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 1.2f);
        return (baseTex, normTex, metalTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateDuckTactical(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Moleteado táctico en diamante (knurling)
                bool knurl = ((x + y) % 8 < 4) ^ ((x - y + 1000) % 8 < 4);
                float height = knurl ? 0.7f : 0.3f;
                heights[x, y] = height;

                Color c = new Color(0.12f, 0.13f, 0.15f);
                Color ec = Color.black;

                // Líneas de visera táctica cian
                if (y > h * 0.45f && y < h * 0.55f)
                {
                    ec = new Color(0.0f, 0.95f, 1.0f);
                    c = ec;
                }

                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(180, 240, 0, 170);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.0f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateSupercomputer(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool rackSlot = (y % 32 < 2);
                float height = rackSlot ? 0.2f : 0.65f;
                heights[x, y] = height;

                Color c = new Color(0.08f, 0.09f, 0.10f);
                Color ec = Color.black;

                // Matriz de LEDs de telemetría (Cian, Ámbar, Blanco)
                if ((y % 8 == 4) && (x % 16 < 4))
                {
                    int ledType = (x + y) % 7;
                    if (ledType == 0) ec = new Color(0.0f, 1.0f, 1.0f);
                    else if (ledType == 1) ec = new Color(1.0f, 0.7f, 0.1f);
                    else if (ledType == 2) ec = Color.white;
                    c = ec;
                }

                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(200, 240, 0, 190);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.2f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateHoloDisc(int w, int h)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (w * 0.5f);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                Color c = new Color(0.04f, 0.05f, 0.07f);
                Color ec = Color.black;
                float height = 0.5f;

                // Anillos de energía holográfica
                float ring = Mathf.Sin(dist * Mathf.PI * 12f);
                if (ring > 0.65f && dist < 0.95f)
                {
                    ec = new Color(0.0f, 0.95f, 1.0f);
                    c = ec;
                    height = 0.85f;
                }

                // Runas / Glifos radiales
                if (dist > 0.7f && dist < 0.85f)
                {
                    float glyph = Mathf.Sin(angle * 24f);
                    if (glyph > 0.8f)
                    {
                        ec = new Color(1.0f, 0.85f, 0.2f); // Glifos dorados
                        c = ec;
                    }
                }

                heights[x, y] = height;
                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(230, 255, 0, 230);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.0f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    public static (Texture2D baseMap, Texture2D normMap, Texture2D metalMap, Texture2D emitMap) GenerateButtonIcon(int w, int h, Color color, string symbol)
    {
        Texture2D baseTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D metalTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Texture2D emitTex = new Texture2D(w, h, TextureFormat.RGBA32, true);
        float[,] heights = new float[w, h];

        Color32[] basePix = new Color32[w * h];
        Color32[] metalPix = new Color32[w * h];
        Color32[] emitPix = new Color32[w * h];

        float cx = w * 0.5f;
        float cy = h * 0.5f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / (w * 0.5f);

                Color c = new Color(0.12f, 0.13f, 0.16f);
                Color ec = Color.black;
                float height = 0.5f;

                // Anillo de borde emisivo del botón
                if (dist > 0.78f && dist < 0.92f)
                {
                    c = color * 0.8f;
                    ec = color * 0.9f;
                    height = 0.85f;
                }
                else if (dist <= 0.78f)
                {
                    // Centro del botón con color tintado sutil
                    c = Color.Lerp(c, color, 0.25f);
                    ec = color * 0.3f;
                }

                heights[x, y] = height;
                basePix[y * w + x] = c;
                emitPix[y * w + x] = ec;
                metalPix[y * w + x] = new Color32(180, 240, 0, 190);
            }
        }

        baseTex.SetPixels32(basePix);
        baseTex.Apply(true);
        metalTex.SetPixels32(metalPix);
        metalTex.Apply(true);
        emitTex.SetPixels32(emitPix);
        emitTex.Apply(true);

        Texture2D normTex = HeightToNormalMap(heights, 2.2f);
        return (baseTex, normTex, metalTex, emitTex);
    }

    // =====================================================================
    // UTILIDADES MATEMÁTICAS PROCEDURALES
    // =====================================================================

    public static Texture2D HeightToNormalMap(float[,] heights, float strength = 2.0f)
    {
        int w = heights.GetLength(0);
        int h = heights.GetLength(1);
        Texture2D norm = new Texture2D(w, h, TextureFormat.RGBA32, true);
        Color32[] pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            int yPrev = (y - 1 + h) % h;
            int yNext = (y + 1) % h;
            for (int x = 0; x < w; x++)
            {
                int xPrev = (x - 1 + w) % w;
                int xNext = (x + 1) % w;

                // Filtro de gradiente Sobel 3x3 en espacio toroidal
                float dX = (heights[xNext, yPrev] + 2f * heights[xNext, y] + heights[xNext, yNext])
                         - (heights[xPrev, yPrev] + 2f * heights[xPrev, y] + heights[xPrev, yNext]);
                float dY = (heights[xPrev, yNext] + 2f * heights[x, yNext] + heights[xNext, yNext])
                         - (heights[xPrev, yPrev] + 2f * heights[x, yPrev] + heights[xNext, yPrev]);

                Vector3 n = new Vector3(-dX * strength, -dY * strength, 1.0f).normalized;

                byte r = (byte)Mathf.Clamp((n.x * 0.5f + 0.5f) * 255f, 0f, 255f);
                byte g = (byte)Mathf.Clamp((n.y * 0.5f + 0.5f) * 255f, 0f, 255f);
                byte b = (byte)Mathf.Clamp((n.z * 0.5f + 0.5f) * 255f, 0f, 255f);

                pixels[y * w + x] = new Color32(r, g, b, 255);
            }
        }

        norm.SetPixels32(pixels);
        norm.Apply(true);
        return norm;
    }

    private static float MathDiamondDist(float u, float v, float angleRad)
    {
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        float ru = u * cos - v * sin;
        float rv = u * sin + v * cos;
        // Elipse estirada en proporción 3.2 a 1
        return Mathf.Sqrt((ru * ru * 1.8f) + (rv * rv * 14.0f));
    }

    private static (float dist, bool isBorder) CalcHexGrid(int x, int y, float r)
    {
        float h = r * Mathf.Sqrt(3f);
        float px = x / (r * 1.5f);
        float py = y / h;
        float rx = Mathf.Repeat(px, 1.0f) - 0.5f;
        float ry = Mathf.Repeat(py, 1.0f) - 0.5f;
        float d = Mathf.Sqrt(rx * rx + ry * ry);
        bool border = d > 0.40f && d < 0.49f;
        return (d, border);
    }

    private static void ApplyStencilDecal(Color32[] pixels, int w, int h, string text)
    {
        // Pinta un chevron táctico y marca de pelotón en el cuadrante superior
        int startX = w / 2 - 40;
        int startY = h / 2 - 40;
        for (int y = 0; y < 80; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                int px = startX + x;
                int py = startY + y;
                if (px < 0 || px >= w || py < 0 || py >= h) continue;

                // Chevron militar en V
                float vx = (x - 40f);
                float vy = y;
                if (Mathf.Abs(vy - Mathf.Abs(vx) * 1.2f) < 8.0f && vy < 60f)
                {
                    pixels[py * w + px] = new Color32(230, 235, 240, 255);
                }
            }
        }
    }
}
