// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "work/AC_ProjectileCharging.h"
#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable : 4883)
#endif
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeAC_ProjectileCharging() {}
// Cross Module References
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileCharging_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileCharging();
	ENGINE_API UClass* Z_Construct_UClass_AActor();
	UPackage* Z_Construct_UPackage__Script_work();
	ENGINE_API UClass* Z_Construct_UClass_USphereComponent_NoRegister();
// End Cross Module References
	void AAC_ProjectileCharging::StaticRegisterNativesAAC_ProjectileCharging()
	{
	}
	UClass* Z_Construct_UClass_AAC_ProjectileCharging_NoRegister()
	{
		return AAC_ProjectileCharging::StaticClass();
	}
	struct Z_Construct_UClass_AAC_ProjectileCharging_Statics
	{
		static UObject* (*const DependentSingletons[])();
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam Class_MetaDataParams[];
#endif
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_collisionComponent_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_collisionComponent;
		static const UE4CodeGen_Private::FPropertyParamsBase* const PropPointers[];
		static const FCppClassTypeInfoStatic StaticCppClassTypeInfo;
		static const UE4CodeGen_Private::FClassParams ClassParams;
	};
	UObject* (*const Z_Construct_UClass_AAC_ProjectileCharging_Statics::DependentSingletons[])() = {
		(UObject* (*)())Z_Construct_UClass_AActor,
		(UObject* (*)())Z_Construct_UPackage__Script_work,
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileCharging_Statics::Class_MetaDataParams[] = {
		{ "IncludePath", "AC_ProjectileCharging.h" },
		{ "ModuleRelativePath", "AC_ProjectileCharging.h" },
	};
#endif
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileCharging_Statics::NewProp_collisionComponent_MetaData[] = {
		{ "Category", "Projectile" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "AC_ProjectileCharging.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_AAC_ProjectileCharging_Statics::NewProp_collisionComponent = { "collisionComponent", nullptr, (EPropertyFlags)0x00100000000b0009, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AAC_ProjectileCharging, collisionComponent), Z_Construct_UClass_USphereComponent_NoRegister, METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileCharging_Statics::NewProp_collisionComponent_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileCharging_Statics::NewProp_collisionComponent_MetaData)) };
	const UE4CodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_AAC_ProjectileCharging_Statics::PropPointers[] = {
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AAC_ProjectileCharging_Statics::NewProp_collisionComponent,
	};
	const FCppClassTypeInfoStatic Z_Construct_UClass_AAC_ProjectileCharging_Statics::StaticCppClassTypeInfo = {
		TCppClassTypeTraits<AAC_ProjectileCharging>::IsAbstract,
	};
	const UE4CodeGen_Private::FClassParams Z_Construct_UClass_AAC_ProjectileCharging_Statics::ClassParams = {
		&AAC_ProjectileCharging::StaticClass,
		"Engine",
		&StaticCppClassTypeInfo,
		DependentSingletons,
		nullptr,
		Z_Construct_UClass_AAC_ProjectileCharging_Statics::PropPointers,
		nullptr,
		UE_ARRAY_COUNT(DependentSingletons),
		0,
		UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileCharging_Statics::PropPointers),
		0,
		0x009000A4u,
		METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileCharging_Statics::Class_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileCharging_Statics::Class_MetaDataParams))
	};
	UClass* Z_Construct_UClass_AAC_ProjectileCharging()
	{
		static UClass* OuterClass = nullptr;
		if (!OuterClass)
		{
			UE4CodeGen_Private::ConstructUClass(OuterClass, Z_Construct_UClass_AAC_ProjectileCharging_Statics::ClassParams);
		}
		return OuterClass;
	}
	IMPLEMENT_CLASS(AAC_ProjectileCharging, 3199899092);
	template<> WORK_API UClass* StaticClass<AAC_ProjectileCharging>()
	{
		return AAC_ProjectileCharging::StaticClass();
	}
	static FCompiledInDefer Z_CompiledInDefer_UClass_AAC_ProjectileCharging(Z_Construct_UClass_AAC_ProjectileCharging, &AAC_ProjectileCharging::StaticClass, TEXT("/Script/work"), TEXT("AAC_ProjectileCharging"), false, nullptr, nullptr, nullptr);
	DEFINE_VTABLE_PTR_HELPER_CTOR(AAC_ProjectileCharging);
PRAGMA_ENABLE_DEPRECATION_WARNINGS
#ifdef _MSC_VER
#pragma warning (pop)
#endif
