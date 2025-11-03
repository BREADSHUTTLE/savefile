// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "work/workCharacter.h"
#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable : 4883)
#endif
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeworkCharacter() {}
// Cross Module References
	WORK_API UClass* Z_Construct_UClass_AworkCharacter_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AworkCharacter();
	ENGINE_API UClass* Z_Construct_UClass_ACharacter();
	UPackage* Z_Construct_UPackage__Script_work();
	ENGINE_API UClass* Z_Construct_UClass_UCameraComponent_NoRegister();
	ENGINE_API UClass* Z_Construct_UClass_USpringArmComponent_NoRegister();
	COREUOBJECT_API UClass* Z_Construct_UClass_UClass();
	WORK_API UClass* Z_Construct_UClass_AAC_Projectile_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileCharging_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileSplit_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileReflection_NoRegister();
	WORK_API UClass* Z_Construct_UClass_UW_UserWidget_NoRegister();
// End Cross Module References
	void AworkCharacter::StaticRegisterNativesAworkCharacter()
	{
	}
	UClass* Z_Construct_UClass_AworkCharacter_NoRegister()
	{
		return AworkCharacter::StaticClass();
	}
	struct Z_Construct_UClass_AworkCharacter_Statics
	{
		static UObject* (*const DependentSingletons[])();
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam Class_MetaDataParams[];
#endif
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_SideViewCameraComponent_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_SideViewCameraComponent;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_CameraBoom_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_CameraBoom;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_projectileClass;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileChargingClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_projectileChargingClass;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileSplitClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_projectileSplitClass;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileReflectionClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_projectileReflectionClass;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_widgetMainClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_widgetMainClass;
		static const UE4CodeGen_Private::FPropertyParamsBase* const PropPointers[];
		static const FCppClassTypeInfoStatic StaticCppClassTypeInfo;
		static const UE4CodeGen_Private::FClassParams ClassParams;
	};
	UObject* (*const Z_Construct_UClass_AworkCharacter_Statics::DependentSingletons[])() = {
		(UObject* (*)())Z_Construct_UClass_ACharacter,
		(UObject* (*)())Z_Construct_UPackage__Script_work,
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::Class_MetaDataParams[] = {
		{ "HideCategories", "Navigation" },
		{ "IncludePath", "workCharacter.h" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_SideViewCameraComponent_MetaData[] = {
		{ "AllowPrivateAccess", "true" },
		{ "Category", "Camera" },
		{ "Comment", "/** Side view camera */" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "workCharacter.h" },
		{ "ToolTip", "Side view camera" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_SideViewCameraComponent = { "SideViewCameraComponent", nullptr, (EPropertyFlags)0x00400000000a001d, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, SideViewCameraComponent), Z_Construct_UClass_UCameraComponent_NoRegister, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_SideViewCameraComponent_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_SideViewCameraComponent_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_CameraBoom_MetaData[] = {
		{ "AllowPrivateAccess", "true" },
		{ "Category", "Camera" },
		{ "Comment", "/** Camera boom positioning the camera beside the character */" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "workCharacter.h" },
		{ "ToolTip", "Camera boom positioning the camera beside the character" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_CameraBoom = { "CameraBoom", nullptr, (EPropertyFlags)0x00400000000a001d, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, CameraBoom), Z_Construct_UClass_USpringArmComponent_NoRegister, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_CameraBoom_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_CameraBoom_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileClass_MetaData[] = {
		{ "Category", "Projectile" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileClass = { "projectileClass", nullptr, (EPropertyFlags)0x0014000000010001, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, projectileClass), Z_Construct_UClass_AAC_Projectile_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileClass_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileChargingClass_MetaData[] = {
		{ "Category", "Projectile" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileChargingClass = { "projectileChargingClass", nullptr, (EPropertyFlags)0x0014000000010001, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, projectileChargingClass), Z_Construct_UClass_AAC_ProjectileCharging_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileChargingClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileChargingClass_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileSplitClass_MetaData[] = {
		{ "Category", "Projectile" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileSplitClass = { "projectileSplitClass", nullptr, (EPropertyFlags)0x0014000000010001, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, projectileSplitClass), Z_Construct_UClass_AAC_ProjectileSplit_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileSplitClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileSplitClass_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileReflectionClass_MetaData[] = {
		{ "Category", "Projectile" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileReflectionClass = { "projectileReflectionClass", nullptr, (EPropertyFlags)0x0014000000010001, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, projectileReflectionClass), Z_Construct_UClass_AAC_ProjectileReflection_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileReflectionClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileReflectionClass_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AworkCharacter_Statics::NewProp_widgetMainClass_MetaData[] = {
		{ "AllowPrivateAccess", "TRUE" },
		{ "Category", "Widget" },
		{ "ModuleRelativePath", "workCharacter.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AworkCharacter_Statics::NewProp_widgetMainClass = { "widgetMainClass", nullptr, (EPropertyFlags)0x0014000000000015, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AworkCharacter, widgetMainClass), Z_Construct_UClass_UW_UserWidget_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::NewProp_widgetMainClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::NewProp_widgetMainClass_MetaData)) };
	const UE4CodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_AworkCharacter_Statics::PropPointers[] = {
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_SideViewCameraComponent,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_CameraBoom,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileClass,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileChargingClass,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileSplitClass,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_projectileReflectionClass,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AworkCharacter_Statics::NewProp_widgetMainClass,
	};
	const FCppClassTypeInfoStatic Z_Construct_UClass_AworkCharacter_Statics::StaticCppClassTypeInfo = {
		TCppClassTypeTraits<AworkCharacter>::IsAbstract,
	};
	const UE4CodeGen_Private::FClassParams Z_Construct_UClass_AworkCharacter_Statics::ClassParams = {
		&AworkCharacter::StaticClass,
		"Game",
		&StaticCppClassTypeInfo,
		DependentSingletons,
		nullptr,
		Z_Construct_UClass_AworkCharacter_Statics::PropPointers,
		nullptr,
		UE_ARRAY_COUNT(DependentSingletons),
		0,
		UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::PropPointers),
		0,
		0x008000A4u,
		METADATA_PARAMS(Z_Construct_UClass_AworkCharacter_Statics::Class_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UClass_AworkCharacter_Statics::Class_MetaDataParams))
	};
	UClass* Z_Construct_UClass_AworkCharacter()
	{
		static UClass* OuterClass = nullptr;
		if (!OuterClass)
		{
			UE4CodeGen_Private::ConstructUClass(OuterClass, Z_Construct_UClass_AworkCharacter_Statics::ClassParams);
		}
		return OuterClass;
	}
	IMPLEMENT_CLASS(AworkCharacter, 3662268031);
	template<> WORK_API UClass* StaticClass<AworkCharacter>()
	{
		return AworkCharacter::StaticClass();
	}
	static FCompiledInDefer Z_CompiledInDefer_UClass_AworkCharacter(Z_Construct_UClass_AworkCharacter, &AworkCharacter::StaticClass, TEXT("/Script/work"), TEXT("AworkCharacter"), false, nullptr, nullptr, nullptr);
	DEFINE_VTABLE_PTR_HELPER_CTOR(AworkCharacter);
PRAGMA_ENABLE_DEPRECATION_WARNINGS
#ifdef _MSC_VER
#pragma warning (pop)
#endif
