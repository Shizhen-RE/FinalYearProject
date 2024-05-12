package com.nearme.nearme_backend.service;

public interface UserAuthService {
    public String checkAuth(String token) throws Exception;
}
