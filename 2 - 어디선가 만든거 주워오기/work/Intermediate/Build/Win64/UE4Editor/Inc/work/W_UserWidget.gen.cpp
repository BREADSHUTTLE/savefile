// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/GeneratedCppIncludes.h"
#include "work/W_UserWidget.h"
#ifdef _MSC_VER
#pragma warning (push)
#pragma warning (disable : 4883)
#endif
PRAGMA_DISABLE_DEPRECATION_WARNINGS
void EmptyLinkFunctionForGeneratedCodeW_UserWidget() {}
// Cross Module References
	WORK_API UClass* Z_Construct_UClass_UW_UserWidget_NoRegister();
	WORK_API UClass* Z_Construct_UClass_UW_UserWidget();
	UMG_API UClass* Z_Construct_UClass_UUserWidget();
	UPackage* Z_Construct_UPackage__Script_work();
	UMG_API UClass* Z_Construct_UClass_UProgressBar_NoRegister();
	UMG_API UClass* Z_Construct_UClass_UButton_NoRegister();
	UMG_API UClass* Z_Construct_UClass_UTextBlock_NoRegister();
// End Cross Module References
	DEFINE_FUNCTION(UW_UserWidget::execResetCount)
	{
		P_FINISH;
		P_NATIVE_BEGIN;
		P_THIS->ResetCount();
		P_NATIVE_END;
	}
	void UW_UserWidget::StaticRegisterNativesUW_UserWidget()
	{
		UClass* Class = UW_UserWidget::StaticClass();
		static const FNameNativePtrPair Funcs[] = {
			{ "ResetCount", &UW_UserWidget::execResetCount },
		};
		FNativeFunctionRegistrar::RegisterFunctions(Class, Funcs, UE_ARRAY_COUNT(Funcs));
	}
	struct Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics
	{
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam Function_MetaDataParams[];
#endif
		static const UE4CodeGen_Private::FFunctionParams FuncParams;
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics::Function_MetaDataParams[] = {
		{ "Comment", "// ?\xd8\xb4? ?\xd4\xbc??? ????????\xc6\xae?????? \xc8\xa3?? ?????\xcf\xb0? ????\n" },
		{ "ModuleRelativePath", "W_UserWidget.h" },
		{ "ToolTip", "?\xd8\xb4? ?\xd4\xbc??? ????????\xc6\xae?????? \xc8\xa3?? ?????\xcf\xb0? ????" },
	};
#endif
	const UE4CodeGen_Private::FFunctionParams Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics::FuncParams = { (UObject*(*)())Z_Construct_UClass_UW_UserWidget, nullptr, "ResetCount", nullptr, nullptr, 0, nullptr, 0, RF_Public|RF_Transient|RF_MarkAsNative, (EFunctionFlags)0x04020401, 0, 0, METADATA_PARAMS(Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics::Function_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics::Function_MetaDataParams)) };
	UFunction* Z_Construct_UFunction_UW_UserWidget_ResetCount()
	{
		static UFunction* ReturnFunction = nullptr;
		if (!ReturnFunction)
		{
			UE4CodeGen_Private::ConstructUFunction(ReturnFunction, Z_Construct_UFunction_UW_UserWidget_ResetCount_Statics::FuncParams);
		}
		return ReturnFunction;
	}
	UClass* Z_Construct_UClass_UW_UserWidget_NoRegister()
	{
		return UW_UserWidget::StaticClass();
	}
	struct Z_Construct_UClass_UW_UserWidget_Statics
	{
		static UObject* (*const DependentSingletons[])();
		static const FClassFunctionLinkInfo FuncInfo[];
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam Class_MetaDataParams[];
#endif
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_chargingBar_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_chargingBar;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_resetButton_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_resetButton;
#if WITH_METADATA
		static const UE4CodeGen_Private::FMetaDataPairParam NewProp_projectileCount_MetaData[];
#endif
		static const UE4CodeGen_Private::FObjectPropertyParams NewProp_projectileCount;
		static const UE4CodeGen_Private::FPropertyParamsBase* const PropPointers[];
		static const FCppClassTypeInfoStatic StaticCppClassTypeInfo;
		static const UE4CodeGen_Private::FClassParams ClassParams;
	};
	UObject* (*const Z_Construct_UClass_UW_UserWidget_Statics::DependentSingletons[])() = {
		(UObject* (*)())Z_Construct_UClass_UUserWidget,
		(UObject* (*)())Z_Construct_UPackage__Script_work,
	};
	const FClassFunctionLinkInfo Z_Construct_UClass_UW_UserWidget_Statics::FuncInfo[] = {
		{ &Z_Construct_UFunction_UW_UserWidget_ResetCount, "ResetCount" }, // 1343593498
	};
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_UW_UserWidget_Statics::Class_MetaDataParams[] = {
		{ "Comment", "/**\n * \n */" },
		{ "IncludePath", "W_UserWidget.h" },
		{ "ModuleRelativePath", "W_UserWidget.h" },
	};
#endif
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_UW_UserWidget_Statics::NewProp_chargingBar_MetaData[] = {
		{ "BindWidget", "" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "W_UserWidget.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_UW_UserWidget_Statics::NewProp_chargingBar = { "chargingBar", nullptr, (EPropertyFlags)0x0020080000080008, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(UW_UserWidget, chargingBar), Z_Construct_UClass_UProgressBar_NoRegister, METADATA_PARAMS(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_chargingBar_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_chargingBar_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_UW_UserWidget_Statics::NewProp_resetButton_MetaData[] = {
		{ "BindWidget", "" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "W_UserWidget.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_UW_UserWidget_Statics::NewProp_resetButton = { "resetButton", nullptr, (EPropertyFlags)0x0020080000080008, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(UW_UserWidget, resetButton), Z_Construct_UClass_UButton_NoRegister, METADATA_PARAMS(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_resetButton_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_resetButton_MetaData)) };
#if WITH_METADATA
	const UE4CodeGen_Private::FMetaDataPairParam Z_Construct_UClass_UW_UserWidget_Statics::NewProp_projectileCount_MetaData[] = {
		{ "BindWidget", "" },
		{ "EditInline", "true" },
		{ "ModuleRelativePath", "W_UserWidget.h" },
	};
#endif
	const UE4CodeGen_Private::FObjectPropertyParams Z_Construct_UClass_UW_UserWidget_Statics::NewProp_projectileCount = { "projectileCount", nullptr, (EPropertyFlags)0x0020080000080008, UE4CodeGen_Private::EPropertyGenFlags::Object, RF_Public|RF_Transient|RF_MarkAsNative, 1, STRUCT_OFFSET(UW_UserWidget, projectileCount), Z_Construct_UClass_UTextBlock_NoRegister, METADATA_PARAMS(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_projectileCount_MetaData, UE_ARRAY_COUNT(Z_Construct_UClass_UW_UserWidget_Statics::NewProp_projectileCount_MetaData)) };
	const UE4CodeGen_Private::FPropertyParamsBase* const Z_Construct_UClass_UW_UserWidget_Statics::PropPointers[] = {
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UW_UserWidget_Statics::NewProp_chargingBar,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UW_UserWidget_Statics::NewProp_resetButton,
		(const UE4CodeGen_Private::FPropertyParamsBase*)&Z_Construct_UClass_UW_UserWidget_Statics::NewProp_projectileCount,
	};
	const FCppClassTypeInfoStatic Z_Construct_UClass_UW_UserWidget_Statics::StaticCppClassTypeInfo = {
		TCppClassTypeTraits<UW_UserWidget>::IsAbstract,
	};
	const UE4CodeGen_Private::FClassParams Z_Construct_UClass_UW_UserWidget_Statics::ClassParams = {
		&UW_UserWidget::StaticClass,
		nullptr,
		&StaticCppClassTypeInfo,
		DependentSingletons,
		FuncInfo,
		Z_Construct_UClass_UW_UserWidget_Statics::PropPointers,
		nullptr,
		UE_ARRAY_COUNT(DependentSingletons),
		UE_ARRAY_COUNT(FuncInfo),
		UE_ARRAY_COUNT(Z_Construct_UClass_UW_UserWidget_Statics::PropPointers),
		0,
		0x00B010A0u,
		METADATA_PARAMS(Z_Construct_UClass_UW_UserWidget_Statics::Class_MetaDataParams, UE_ARRAY_COUNT(Z_Construct_UClass_UW_UserWidget_Statics::Class_MetaDataParams))
	};
	UClass* Z_Construct_UClass_UW_UserWidget()
	{
		static UClass* OuterClass = nullptr;
		if (!OuterClass)
		{
			UE4CodeGen_Private::ConstructUClass(OuterClass, Z_Construct_UClass_UW_UserWidget_Statics::ClassParams);
		}
		return OuterClass;
	}
	IMPLEMENT_CLASS(UW_UserWidget, 2271384186);
	template<> WORK_API UClass* StaticClass<UW_UserWidget>()
	{
		return UW_UserWidget::StaticClass();
	}
	static FCompiledInDefer Z_CompiledInDefer_UClass_UW_UserWidget(Z_Construct_UClass_UW_UserWidget, &UW_UserWidget::StaticClass, TEXT("/Script/work"), TEXT("UW_UserWidget"), false, nullptr, nullptr, nullptr);
	DEFINE_VTABLE_PTR_HELPER_CTOR(UW_UserWidget);
PRAGMA_ENABLE_DEPRECATION_WARNINGS
#ifdef _MSC_VER
#pragma warning (pop)
#endif
