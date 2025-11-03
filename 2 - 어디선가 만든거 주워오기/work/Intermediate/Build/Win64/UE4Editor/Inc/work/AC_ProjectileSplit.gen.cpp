// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "work/AC_ProjectileSplit.h"
#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable : 4883)
#endif
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeAC_ProjectileSplit() {}
// Cross Module References
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileSplit_NoRegister();
	WORK_API UClass* Z_Construct_UClass_AAC_ProjectileSplit();
	ENGINE_API UClass* Z_Construct_UClass_AActor();
	UPackage* Z_Construct_UPackage__Script_work();
	ENGINE_API UClass* Z_Construct_UClass_USphereComponent_NoRegister();
	COREUOBJECT_API UClass* Z_Construct_UClass_UClass();
	WORK_API UClass* Z_Construct_UClass_AAC_Projectile_NoRegister();
// End Cross Module References
	void AAC_ProjectileSplit::StaticRegisterNativesAAC_ProjectileSplit()
	{
	}
	UClass* Z_Construct_UClass_AAC_ProjectileSplit_NoRegister()
	{
		return AAC_ProjectileSplit::StaticClass();
	}
	struct Z_Construct_UClass_AAC_ProjectileSplit_Statics
	{
		static UObject* (*const DependentSingletons[])();
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam Class_MetaDataParams[];
#endif
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_collisionComponent_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_collisionComponent;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileClass_MetaData[];
#endif
		static const UE4CodeGen_Private::FClassPropertyParams NewProp_projectileClass;
		static const UE4CodeGen_Private::FPropertyParamsBase* const PropPointers[];
		static const FCppClassTypeInfoStatic StaticCppClassTypeInfo;
		static const UE4CodeGen_Private::FClassParams ClassParams;
	};
	UObject* (*const Z_Construct_UClass_AAC_ProjectileSplit_Statics::DependentSingletons[])() = {
		(UObject* (*)())Z_Construct_UClass_AActor,
		(UObject* (*)())Z_Construct_UPackage__Script_work,
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileSplit_Statics::Class_MetaDataParams[] = {
		{ "IncludePath", "AC_ProjectileSplit.h" },
		{ "ModuleRelativePath", "AC_ProjectileSplit.h" },
	};
#endif
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_collisionComponent_MetaData[] = {
		{ "Category", "Projectile" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "AC_ProjectileSplit.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_collisionComponent = { "collisionComponent", nullptr, (EPropertyFlags)0x00100000000b0009, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AAC_ProjectileSplit, collisionComponent), Z_Construct_UClass_USphereComponent_NoRegister, METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_collisionComponent_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_collisionComponent_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_projectileClass_MetaData[] = {
		{ "Category", "Projectile" },
		{ "ModuleRelativePath", "AC_ProjectileSplit.h" },
	};
#endif
	const UE4CodeGen_Private::FClassPropertyParams Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_projectileClass = { "projectileClass", nullptr, (EPropertyFlags)0x0014000000010001, UE4CodeGen_Private::EPropertyGenFlags::Class, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(AAC_ProjectileSplit, projectileClass), Z_Construct_UClass_AAC_Projectile_NoRegister, Z_Construct_UClass_UClass, METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_projectileClass_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_projectileClass_MetaData)) };
	const UE4CodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_AAC_ProjectileSplit_Statics::PropPointers[] = {
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_collisionComponent,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_AAC_ProjectileSplit_Statics::NewProp_projectileClass,
	};
	const FCppClassTypeInfoStatic Z_Construct_UClass_AAC_ProjectileSplit_Statics::StaticCppClassTypeInfo = {
		TCppClassTypeTraits<AAC_ProjectileSplit>::IsAbstract,
	};
	const UE4CodeGen_Private::FClassParams Z_Construct_UClass_AAC_ProjectileSplit_Statics::ClassParams = {
		&AAC_ProjectileSplit::StaticClass,
		"Engine",
		&StaticCppClassTypeInfo,
		DependentSingletons,
		nullptr,
		Z_Construct_UClass_AAC_ProjectileSplit_Statics::PropPointers,
		nullptr,
		UE_ARRAY_COUNT(DependentSingletons),
		0,
		UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileSplit_Statics::PropPointers),
		0,
		0x009000A4u,
		METADATA_PARAMS(Z_Construct_UClass_AAC_ProjectileSplit_Statics::Class_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UClass_AAC_ProjectileSplit_Statics::Class_MetaDataParams))
	};
	UClass* Z_Construct_UClass_AAC_ProjectileSplit()
	{
		static UClass* OuterClass = nullptr;
		if (!OuterClass)
		{
			UE4CodeGen_Private::ConstructUClass(OuterClass, Z_Construct_UClass_AAC_ProjectileSplit_Statics::ClassParams);
		}
		return OuterClass;
	}
	IMPLEMENT_CLASS(AAC_ProjectileSplit, 1030247276);
	template<> WORK_API UClass* StaticClass<AAC_ProjectileSplit>()
	{
		return AAC_ProjectileSplit::StaticClass();
	}
	static FCompiledInDefer Z_CompiledInDefer_UClass_AAC_ProjectileSplit(Z_Construct_UClass_AAC_ProjectileSplit, &AAC_ProjectileSplit::StaticClass, TEXT("/Script/work"), TEXT("AAC_ProjectileSplit"), false, nullptr, nullptr, nullptr);
	DEFINE_VTABLE_PTR_HELPER_CTOR(AAC_ProjectileSplit);
PRAGMA_ENABLE_DEPRECATION_WARNINGS
#ifdef _MSC_VER
#pragma warning (pop)
#endif
