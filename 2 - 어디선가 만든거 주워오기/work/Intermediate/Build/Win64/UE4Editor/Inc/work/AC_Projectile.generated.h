// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef WORK_AC_Projectile_generated_h
#error "AC_Projectile.generated.h already included, missing '#pragma once' in AC_Projectile.h"
#endif
#define WORK_AC_Projectile_generated_h

#define work_Source_work_AC_Projectile_h_14_SPARSE_DATA
#define work_Source_work_AC_Projectile_h_14_RPC_WRAPPERS
#define work_Source_work_AC_Projectile_h_14_RPC_WRAPPERS_NO_PURE_DECLS
#define work_Source_work_AC_Projectile_h_14_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesAAC_Projectile(); \
	friend struct Z_Construct_UClass_AAC_Projectile_Statics; \
public: \
	DECLARE_CLASS(AAC_Projectile, AActor, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(AAC_Projectile)


#define work_Source_work_AC_Projectile_h_14_INCLASS \
private: \
	static void StaticRegisterNativesAAC_Projectile(); \
	friend struct Z_Construct_UClass_AAC_Projectile_Statics; \
public: \
	DECLARE_CLASS(AAC_Projectile, AActor, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(AAC_Projectile)


#define work_Source_work_AC_Projectile_h_14_STANDARD_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API AAC_Projectile(const FObjectInitializer& ObjectInitializer); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(AAC_Projectile) \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, AAC_Projectile); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(AAC_Projectile); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API AAC_Projectile(AAC_Projectile&&); \
	NO_API AAC_Projectile(const AAC_Projectile&); \
public:


#define work_Source_work_AC_Projectile_h_14_ENHANCED_CONSTRUCTORS \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API AAC_Projectile(AAC_Projectile&&); \
	NO_API AAC_Projectile(const AAC_Projectile&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, AAC_Projectile); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(AAC_Projectile); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(AAC_Projectile)


#define work_Source_work_AC_Projectile_h_14_PRIVATE_PROPERTY_OFFSET \
	FORCEINLINE static uint32 __PPO__moveDelay() { return STRUCT_OFFSET(AAC_Projectile, moveDelay); } \
	FORCEINLINE static uint32 __PPO__moveSpeed() { return STRUCT_OFFSET(AAC_Projectile, moveSpeed); }


#define work_Source_work_AC_Projectile_h_11_PROLOG
#define work_Source_work_AC_Projectile_h_14_GENERATED_BODY_LEGACY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_AC_Projectile_h_14_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_AC_Projectile_h_14_SPARSE_DATA \
	work_Source_work_AC_Projectile_h_14_RPC_WRAPPERS \
	work_Source_work_AC_Projectile_h_14_INCLASS \
	work_Source_work_AC_Projectile_h_14_STANDARD_CONSTRUCTORS \
public: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


#define work_Source_work_AC_Projectile_h_14_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_AC_Projectile_h_14_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_AC_Projectile_h_14_SPARSE_DATA \
	work_Source_work_AC_Projectile_h_14_RPC_WRAPPERS_NO_PURE_DECLS \
	work_Source_work_AC_Projectile_h_14_INCLASS_NO_PURE_DECLS \
	work_Source_work_AC_Projectile_h_14_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> WORK_API UClass* StaticClass<class AAC_Projectile>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID work_Source_work_AC_Projectile_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
