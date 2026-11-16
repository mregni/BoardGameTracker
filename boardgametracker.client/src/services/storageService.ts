import type {
	ArrangeStorage,
	AssignStorage,
	CreateStorageLocation,
	MoveStorageLocation,
	StorageLocation,
	UpdateBoxOrientation,
	UpdateStorageLocation,
} from "../models";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "storage";

export const getStorageLocationsCall = (): Promise<StorageLocation[]> =>
	axiosInstance.get<StorageLocation[]>(domain).then((response) => response.data);

export const addStorageLocationCall = (storageLocation: CreateStorageLocation): Promise<StorageLocation> =>
	axiosInstance.post<StorageLocation>(domain, storageLocation).then((response) => response.data);

export const updateStorageLocationCall = (
	id: number,
	storageLocation: UpdateStorageLocation,
): Promise<StorageLocation> =>
	axiosInstance.put<StorageLocation>(`${domain}/${id}`, storageLocation).then((response) => response.data);

export const moveStorageLocationCall = (id: number, move: MoveStorageLocation): Promise<StorageLocation> =>
	axiosInstance.put<StorageLocation>(`${domain}/${id}/move`, move).then((response) => response.data);

export const deleteStorageLocationCall = (id: number): Promise<void> => axiosInstance.delete(`${domain}/${id}`);

export const assignStorageCall = (assign: AssignStorage): Promise<void> =>
	axiosInstance.put(`${domain}/assign`, assign);

export const arrangeStorageCall = (arrange: ArrangeStorage): Promise<void> =>
	axiosInstance.put(`${domain}/arrange`, arrange);

export const updateBoxOrientationCall = (orientation: UpdateBoxOrientation): Promise<void> =>
	axiosInstance.put(`${domain}/orientation`, orientation);
