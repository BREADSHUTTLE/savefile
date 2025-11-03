// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "work/AC_ProjectileReflection.h"
#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable : 4883)
#endif
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeAC_ProjectileReflection() {}
// Cross Module References
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileReflection_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileReflection();
	ENGINE_API UClass* Z_Construct_UClass_AActor();
	UPackage* Z_Construct_UPackage__Script_work();
	ENGINE_API UClass* Z_Construct_UClass_USphereComponent_NoRegister();
// End Cross Module References
	void AAC_ProjectileReflection::StaticRegisterNativesAAC_ProjectileReflection()
	{
	}
	UClass* Z_Construct_UClass_AAC_ProjectileReflection_NoRegister()
	{
		return AAC_ProjectileReflection::StaticClass();
	}
	struct Z_Construct_UClass_AAC_ProjectileReflection_Statics
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
	UObject* (*const Z_Construct_UClass_AAC_ProjectileReflection_Statics::DependentSingletons[])() = {
		(UObject* (*)())Z_Construct_UClass_AActor,
		(UObject* (*)())Z_Construct_UPackage__Script_work,
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileReflection_Statics::Class_MetaDataParams[] = {
		{ "IncludePath", "AC_ProjectileReflection.h" },
		{ "ModuleRelativePath", "AC_ProjectileReflection.h" },
	};
#endif
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileReflection_Statics::NewProp_collisionComponent_MetaData[] = {
		{ "Category", "Projectile" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "AC_ProjectileReflection.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_AAC_ProjectileReflection_Statics::NewProp_collisionComponent = { "collisionComponent", nullptr, (EPropertyFlags)0x00100000000b0009, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AAC_ProjectileReflection, collisionComponent), Z_Construct_UClass_USphereComponent_NoRegister, METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileReflection_Statics::NewProp_collisionComponent_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileReflection_Statics::NewProp_collisionComponent_MetaData)) };
	const UE4CodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_AAC_ProjectileReflection_Statics::PropPointers[] = {
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AAC_ProjectileReflection_Statics::NewProp_collisionComponent,
	};
	const FCppClassTypeInfoStatic Z_Construct_UClass_AAC_ProjectileReflection_Statics::StaticCppClassTypeInfo = {
		TCppClassTypeTraits<AAC_ProjectileReflection>::IsAbstract,
	};
	const UE4CodeGen_Private::FClassParams Z_Construct_UClass_AAC_ProjectileReflection_Statics::ClassParams = {
		&AAC_ProjectileReflection::StaticClass,
		"Engine",
		&StaticCppClassTypeInfo,
		DependentSingletons,
		nullptr,
		Z_Construct_UClass_AAC_ProjectileReflection_Statics::PropPointers,
		nullptr,
		UE_ARRAY_COUNT(DependentSingletons),
		0,
		UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileReflection_Statics::PropPointers),
		0,
		0x009000A4u,
		METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileReflection_Statics::Class_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileReflection_Statics::Class_MetaDataParams))
	};
	UClass* Z_Construct_UClass_AAC_ProjectileReflection()
	{
		static UClass* OuterClass = nullptr;
		if (!OuterClass)
		{
			UE4CodeGen_Private::ConstructUClass(OuterClass, Z_Construct_UClass_AAC_ProjectileReflection_Statics::ClassParams);
		}
		return OuterClass;
	}
	IMPLEMENT_CLASS(AAC_ProjectileReflection, 1973836616);
	template<> WORK_API UClass* StaticClass<AAC_ProjectileReflection>()
	{
		return AAC_ProjectileReflection::StaticClass();
	}
	static FCompiledInDefer Z_CompiledInDefer_UClass_AAC_ProjectileReflection(Z_Construct_UClass_AAC_ProjectileReflection, &AAC_ProjectileReflection::StaticClass, TEXT("/Script/work"), TEXT("AAC_ProjectileReflection"), false, nullptr, nullptr, nullptr);
	DEFINE_VTABLE_PTR_HELPER_CTOR(AAC_ProjectileReflection);
PRAGMA_ENABLE_DEPRECATION_WARNINGS
#ifdef _MSC_VER
#pragma warning (pop)
#endif
