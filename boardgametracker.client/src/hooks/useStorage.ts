import { useMutation, useQuery } from "@tanstack/react-query";
import { useToasts } from "@/routes/-hooks/useToasts";
import { getLocations } from "@/services/queries/locations";
import { getStorageLocations } from "@/services/queries/storage";
import {
	addStorageLocationCall,
	arrangeStorageCall,
	assignStorageCall,
	deleteStorageLocationCall,
	moveStorageLocationCall,
	updateBoxOrientationCall,
	updateStorageLocationCall,
} from "@/services/storageService";
import { apiErrorMessage } from "@/utils/errorUtils";
import type { MoveStorageLocation, UpdateStorageLocation } from "../models";
import { useQueryInvalidator } from "./useQueryInvalidator";

export const useStorageData = () => {
	const nodes = useQuery(getStorageLocations());
	const locations = useQuery(getLocations());

	return {
		nodes: nodes.data ?? [],
		locations: locations.data ?? [],
		isLoading: nodes.isLoading || locations.isLoading,
	};
};

export const useStorageActions = () => {
	const invalidator = useQueryInvalidator();
	const { successToast, errorToast } = useToasts();
	const shared = {
		onError: (error: unknown) => errorToast(apiErrorMessage(error, "storage:notifications.failed")),
		onSettled: () => invalidator.invalidateStorage(),
	};

	const create = useMutation({ mutationFn: addStorageLocationCall, ...shared });
	const update = useMutation({
		mutationFn: ({ id, ...rest }: UpdateStorageLocation & { id: number }) => updateStorageLocationCall(id, rest),
		...shared,
	});
	const move = useMutation({
		mutationFn: ({ id, ...rest }: MoveStorageLocation & { id: number }) => moveStorageLocationCall(id, rest),
		onSuccess: () => successToast("storage:notifications.moved"),
		...shared,
	});
	const remove = useMutation({
		mutationFn: deleteStorageLocationCall,
		onSuccess: () => successToast("storage:notifications.deleted"),
		...shared,
	});
	const assign = useMutation({ mutationFn: assignStorageCall, ...shared });
	const arrange = useMutation({ mutationFn: arrangeStorageCall, ...shared });
	const orientation = useMutation({ mutationFn: updateBoxOrientationCall, ...shared });

	return {
		createStorageLocation: create.mutateAsync,
		updateStorageLocation: update.mutateAsync,
		moveStorageLocation: move.mutateAsync,
		deleteStorageLocation: remove.mutateAsync,
		assignStorage: assign.mutateAsync,
		arrangeStorage: arrange.mutateAsync,
		updateBoxOrientation: orientation.mutateAsync,
		isPending:
			create.isPending ||
			update.isPending ||
			move.isPending ||
			remove.isPending ||
			assign.isPending ||
			arrange.isPending ||
			orientation.isPending,
	};
};
