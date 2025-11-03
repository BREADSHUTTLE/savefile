// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef WORK_W_UserWidget_generated_h
#error "W_UserWidget.generated.h already included, missing '#pragma once' in W_UserWidget.h"
#endif
#define WORK_W_UserWidget_generated_h

#define work_Source_work_W_UserWidget_h_19_SPARSE_DATA
#define work_Source_work_W_UserWidget_h_19_RPC_WRAPPERS \
 \
	DECLARE_FUNCTION(execResetCount);


#define work_Source_work_W_UserWidget_h_19_RPC_WRAPPERS_NO_PURE_DECLS \
 \
	DECLARE_FUNCTION(execResetCount);


#define work_Source_work_W_UserWidget_h_19_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesUW_UserWidget(); \
	friend struct Z_Construct_UClass_UW_UserWidget_Statics; \
public: \
	DECLARE_CLASS(UW_UserWidget, UUserWidget, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(UW_UserWidget)


#define work_Source_work_W_UserWidget_h_19_INCLASS \
private: \
	static void StaticRegisterNativesUW_UserWidget(); \
	friend struct Z_Construct_UClass_UW_UserWidget_Statics; \
public: \
	DECLARE_CLASS(UW_UserWidget, UUserWidget, COMPILED_IN_FLAGS(0), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(UW_UserWidget)


#define work_Source_work_W_UserWidget_h_19_STANDARD_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UW_UserWidget(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UW_UserWidget) \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UW_UserWidget); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UW_UserWidget); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API UW_UserWidget(UW_UserWidget&&); \
	NO_API UW_UserWidget(const UW_UserWidget&); \
public:


#define work_Source_work_W_UserWidget_h_19_ENHANCED_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API UW_UserWidget(const FObjectInitializer& ObjectInitializer = FObjectInitializer::Get()) : Super(ObjectInitializer) { }; \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API UW_UserWidget(UW_UserWidget&&); \
	NO_API UW_UserWidget(const UW_UserWidget&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, UW_UserWidget); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(UW_UserWidget); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(UW_UserWidget)


#define work_Source_work_W_UserWidget_h_19_PRIVATE_PROPERTY_OFFSET \
	FORCEINLINE static uint32 __PPO__chargingBar() { return STRUCT_OFFSET(UW_UserWidget, chargingBar); } \
	FORCEINLINE static uint32 __PPO__resetButton() { return STRUCT_OFFSET(UW_UserWidget, resetButton); } \
	FORCEINLINE static uint32 __PPO__projectileCount() { return STRUCT_OFFSET(UW_UserWidget, projectileCount); }


#define work_Source_work_W_UserWidget_h_16_PROLOG
#define work_Source_work_W_UserWidget_h_19_GENERATED_BODY_LEGACY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_W_UserWidget_h_19_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_W_UserWidget_h_19_SPARSE_DATA \
	work_Source_work_W_UserWidget_h_19_RPC_WRAPPERS \
	work_Source_work_W_UserWidget_h_19_INCLASS \
	work_Source_work_W_UserWidget_h_19_STANDARD_CONSTRUCTORS \
public: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


#define work_Source_work_W_UserWidget_h_19_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_W_UserWidget_h_19_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_W_UserWidget_h_19_SPARSE_DATA \
	work_Source_work_W_UserWidget_h_19_RPC_WRAPPERS_NO_PURE_DECLS \
	work_Source_work_W_UserWidget_h_19_INCLASS_NO_PURE_DECLS \
	work_Source_work_W_UserWidget_h_19_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> WORK_API UClass* StaticClass<class UW_UserWidget>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID work_Source_work_W_UserWidget_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
