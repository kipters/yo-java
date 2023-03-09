package dev.kipters.yodemo.controller;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import dev.kipters.yodemo.dto.AuthRequest;
import dev.kipters.yodemo.service.IUserService;

@RestController
@RequestMapping("/auth")
public class AuthController {
    
    @Autowired
    private IUserService userService;

    @PostMapping("/register")
    public ResponseEntity<?> register(@RequestBody AuthRequest body) {
        userService.addUser(body.username(), body.password());
        return new ResponseEntity<>(HttpStatus.CREATED);
    }
}
