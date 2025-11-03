// Copyright Epic Games, Inc. All Rights Reserved.

#include "workGameMode.h"
#include "workCharacter.h"
#include "UObject/ConstructorHelpers.h"

AworkGameMode::AworkGameMode()
{
	// set default pawn class to our Blueprinted character
	static ConstructorHelpers::FClassFinder<APawn> PlayerPawnBPClass(TEXT("/Game/SideScrollerCPP/Blueprints/SideScrollerCharacter"));
	if (PlayerPawnBPClass.Class != nullptr)
	{
		DefaultPawnClass = PlayerPawnBPClass.Class;
	}
}
