import type { CreateLocation, Location, LocationStatistics, Session } from "../models";
import { axiosInstance } from "../utils/axiosInstance";

const domain = "location";

export const getLocationsCall = (): Promise<Location[]> => {
	return axiosInstance.get<Location[]>(domain).then((response) => {
		return response.data;
	});
};

export const getLocationCall = (id: number): Promise<Location> => {
	return axiosInstance.get<Location>(`${domain}/${id}`).then((response) => {
		return response.data;
	});
};

export const getLocationStatisticsCall = (id: number): Promise<LocationStatistics> => {
	return axiosInstance.get<LocationStatistics>(`${domain}/${id}/statistics`).then((response) => {
		return response.data;
	});
};

export const getLocationSessionsCall = (id: number, count?: number): Promise<Session[]> => {
	return axiosInstance.get<Session[]>(`${domain}/${id}/sessions`, { params: { count } }).then((response) => {
		return response.data;
	});
};

export const addLocationCall = (location: CreateLocation): Promise<Location> => {
	return axiosInstance.post<Location>(domain, location).then((response) => {
		return response.data;
	});
};

export const deleteLocationCall = (id: number): Promise<void> => {
	return axiosInstance.delete(`${domain}/${id}`);
};

export const updateLocationCall = (location: Location): Promise<Location> => {
	return axiosInstance.put<Location>(domain, location).then((response) => {
		return response.data;
	});
};
