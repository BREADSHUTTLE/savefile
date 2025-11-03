// Copyright Epic Games, Inc. All Rights Reserved.
/*===========================================================================
	Generated code exported from UnrealHeaderTool.
	DO NOT modify this manually! Edit the corresponding .h files instead!
===========================================================================*/

#include "UObject/ObjectMacros.h"
#include "UObject/ScriptMacros.h"

PRAGMA_DISABLE_DEPRECATION_WARNINGS
#ifdef WORK_workCharacter_generated_h
#error "workCharacter.generated.h already included, missing '#pragma once' in workCharacter.h"
#endif
#define WORK_workCharacter_generated_h

#define work_Source_work_workCharacter_h_12_SPARSE_DATA
#define work_Source_work_workCharacter_h_12_RPC_WRAPPERS
#define work_Source_work_workCharacter_h_12_RPC_WRAPPERS_NO_PURE_DECLS
#define work_Source_work_workCharacter_h_12_INCLASS_NO_PURE_DECLS \
private: \
	static void StaticRegisterNativesAworkCharacter(); \
	friend struct Z_Construct_UClass_AworkCharacter_Statics; \
public: \
	DECLARE_CLASS(AworkCharacter, ACharacter, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(AworkCharacter)


#define work_Source_work_workCharacter_h_12_INCLASS \
private: \
	static void StaticRegisterNativesAworkCharacter(); \
	friend struct Z_Construct_UClass_AworkCharacter_Statics; \
public: \
	DECLARE_CLASS(AworkCharacter, ACharacter, COMPILED_IN_FLAGS(0 | CLASS_Config), CASTCLASS_None, TEXT("/Script/work"), NO_API) \
	DECLARE_SERIALIZER(AworkCharacter)


#define work_Source_work_workCharacter_h_12_STANDARD_CONSTRUCTORS \
	/** Standard constructor, called after all reflected properties have been initialized */ \
	NO_API AworkCharacter(const FObjectInitializer& ObjectInitializer); \
	DEFINE_DEFAULT_OBJECT_INITIALIZER_CONSTRUCTOR_CALL(AworkCharacter) \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, AworkCharacter); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(AworkCharacter); \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API AworkCharacter(AworkCharacter&&); \
	NO_API AworkCharacter(const AworkCharacter&); \
public:


#define work_Source_work_workCharacter_h_12_ENHANCED_CONSTRUCTORS \
private: \
	/** Private move- and copy-constructors, should never be used */ \
	NO_API AworkCharacter(AworkCharacter&&); \
	NO_API AworkCharacter(const AworkCharacter&); \
public: \
	DECLARE_VTABLE_PTR_HELPER_CTOR(NO_API, AworkCharacter); \
DEFINE_VTABLE_PTR_HELPER_CTOR_CALLER(AworkCharacter); \
	DEFINE_DEFAULT_CONSTRUCTOR_CALL(AworkCharacter)


#define work_Source_work_workCharacter_h_12_PRIVATE_PROPERTY_OFFSET \
	FORCEINLINE static uint32 __PPO__SideViewCameraComponent() { return STRUCT_OFFSET(AworkCharacter, SideViewCameraComponent); } \
	FORCEINLINE static uint32 __PPO__CameraBoom() { return STRUCT_OFFSET(AworkCharacter, CameraBoom); }


#define work_Source_work_workCharacter_h_9_PROLOG
#define work_Source_work_workCharacter_h_12_GENERATED_BODY_LEGACY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_workCharacter_h_12_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_workCharacter_h_12_SPARSE_DATA \
	work_Source_work_workCharacter_h_12_RPC_WRAPPERS \
	work_Source_work_workCharacter_h_12_INCLASS \
	work_Source_work_workCharacter_h_12_STANDARD_CONSTRUCTORS \
public: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


#define work_Source_work_workCharacter_h_12_GENERATED_BODY \
PRAGMA_DISABLE_DEPRECATION_WARNINGS \
public: \
	work_Source_work_workCharacter_h_12_PRIVATE_PROPERTY_OFFSET \
	work_Source_work_workCharacter_h_12_SPARSE_DATA \
	work_Source_work_workCharacter_h_12_RPC_WRAPPERS_NO_PURE_DECLS \
	work_Source_work_workCharacter_h_12_INCLASS_NO_PURE_DECLS \
	work_Source_work_workCharacter_h_12_ENHANCED_CONSTRUCTORS \
private: \
PRAGMA_ENABLE_DEPRECATION_WARNINGS


template<> WORK_API UClass* StaticClass<class AworkCharacter>();

#undef CURRENT_FILE_ID
#define CURRENT_FILE_ID work_Source_work_workCharacter_h


PRAGMA_ENABLE_DEPRECATION_WARNINGS
